using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using RSChatApp.Shared.Infrastructure.Mcp.SemanticSearch.Models;
using RSChatApp.Shared.Infrastructure.Mcp.SemanticSearch.Services;

namespace TestRsMcpServer;

/// <summary>
/// Integration tests for IDocumentLookup / DocumentLookup service.
/// Uses a mock IWebHostEnvironment pointing to the real wwwroot/Data directory.
/// </summary>
[TestClass]
public sealed class DocumentLookupIntegrationTests
{
    private static DocumentLookup _documentLookup = null!;

    private const string TestDocumentId = "user_en_RS5.0.pdf";

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext)
    {
        // Resolve the wwwroot path relative to the RSChatApp.Web project
        var webProjectDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "src", "RSChatApp.RSChatApp", "RSChatApp.Web"));
        var wwwrootPath = Path.Combine(webProjectDir, "wwwroot");

        Assert.IsTrue(Directory.Exists(wwwrootPath),
            $"wwwroot directory not found at {wwwrootPath}");
        Assert.IsTrue(File.Exists(Path.Combine(wwwrootPath, "Data", TestDocumentId)),
            $"Test PDF not found: {Path.Combine(wwwrootPath, "Data", TestDocumentId)}");

        var mockEnv = new MockWebHostEnvironment { WebRootPath = wwwrootPath };
        _documentLookup = new DocumentLookup(mockEnv);
    }

    [TestMethod]
    public void DocumentLookup_IsCreated_Successfully()
    {
        Assert.IsNotNull(_documentLookup, "DocumentLookup should be created successfully");
    }

    [TestMethod]
    public void Lookup_ReturnsText_ForValidDocumentAndPage()
    {
        // Act — default mode is RenderingOrder
        var result = _documentLookup.Lookup(TestDocumentId, page: 1);

        // Assert
        Assert.IsNotNull(result, "Lookup should return a result");
        Assert.AreEqual(TestDocumentId, result.DocumentId, "DocumentId should match the requested document");
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Text), "Text content should not be empty for page 1");
        Assert.IsNull(result.Images, "Images should be null when addImages is false (default)");
        Assert.IsTrue(result.Timestamp <= DateTime.Now, "Timestamp should be set");

        Console.WriteLine($"✅ Page 1 text length: {result.Text.Length} characters");
        Console.WriteLine($"   Preview: {result.Text[..Math.Min(200, result.Text.Length)]}...");
    }

    [TestMethod]
    [DataRow(TextExtractionMode.Raw, DisplayName = "Raw mode")]
    [DataRow(TextExtractionMode.RenderingOrder, DisplayName = "RenderingOrder mode")]
    [DataRow(TextExtractionMode.SpatialOrder, DisplayName = "SpatialOrder mode")]
    public void Lookup_ReturnsText_ForAllExtractionModes(TextExtractionMode mode)
    {
        // Act
        var result = _documentLookup.Lookup(TestDocumentId, page: 1, textMode: mode);

        // Assert
        Assert.IsNotNull(result, "Lookup should return a result");
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Text),
            $"Text content should not be empty for mode {mode}");

        Console.WriteLine($"✅ [{mode}] text length: {result.Text.Length} chars");
        Console.WriteLine($"   Preview: {result.Text[..Math.Min(200, result.Text.Length)]}...");
    }

    [TestMethod]
    public void Lookup_LayoutAnalysis_PreservesLineBreaks()
    {
        // Use page 13 which has substantial text content (page 1 may be a title page)
        const int testPage = 13;

        // Act
        var renderResult = _documentLookup.Lookup(TestDocumentId, page: testPage,
            textMode: TextExtractionMode.RenderingOrder);
        var rawResult = _documentLookup.Lookup(TestDocumentId, page: testPage,
            textMode: TextExtractionMode.Raw);

        // Assert — layout analysis should produce block-separated text
        Assert.IsFalse(string.IsNullOrWhiteSpace(renderResult.Text),
            "RenderingOrder text should not be empty");
        Assert.IsTrue(renderResult.Text.Contains('\n'),
            "RenderingOrder text should contain line breaks from block separation");

        Console.WriteLine($"✅ Raw length: {rawResult.Text.Length}, RenderingOrder length: {renderResult.Text.Length}");
        Console.WriteLine($"   Raw newlines: {rawResult.Text.Count(c => c == '\n')}");
        Console.WriteLine($"   RenderingOrder newlines: {renderResult.Text.Count(c => c == '\n')}");
    }

    [TestMethod]
    public void Lookup_ReturnsImages_WhenAddImagesIsTrue()
    {
        // Act
        var result = _documentLookup.Lookup(TestDocumentId, page: 11, addImages: true);

        // Assert
        Assert.IsNotNull(result, "Lookup should return a result");
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Text), "Text content should not be empty");

        if (result.Images is not null && result.Images.Length > 0)
        {
            foreach (var image in result.Images)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(image.ImageData),
                    "ImageData should not be empty");
                Assert.IsTrue(
                    image.MimeType is "image/png" or "image/jpeg",
                    $"MimeType should be image/png or image/jpeg, got: {image.MimeType}");

                // Verify it's valid base64
                var bytes = Convert.FromBase64String(image.ImageData);
                Assert.IsTrue(bytes.Length > 0, "Decoded image bytes should not be empty");
            }

            Console.WriteLine($"✅ Found {result.Images.Length} image(s) on page 1");
            foreach (var img in result.Images)
            {
                var sizeKb = (img.ImageData.Length * 3) / 4 / 1024;
                Console.WriteLine($"   - {img.MimeType}, ~{sizeKb} KB");
            }
        }
        else
        {
            Console.WriteLine("ℹ️ Page 1 has no extractable images (this is valid)");
        }
    }

    [TestMethod]
    public void Lookup_ImagesAreCompressed_WhenLarge()
    {
        // Act — request images from multiple pages to find one with images
        DocumentLookupResult? resultWithImages = null;
        for (int page = 1; page <= 5; page++)
        {
            var result = _documentLookup.Lookup(TestDocumentId, page, addImages: true);
            if (result.Images is { Length: > 0 })
            {
                resultWithImages = result;
                Console.WriteLine($"✅ Found images on page {page}");
                break;
            }
        }

        if (resultWithImages?.Images is null)
        {
            Console.WriteLine("ℹ️ No images found in first 5 pages — compression test skipped");
            return;
        }

        // Assert — all images should be under the 500 KB base64 threshold
        const int maxBase64Length = 512_000;
        foreach (var image in resultWithImages.Images)
        {
            Assert.IsTrue(image.ImageData.Length <= maxBase64Length,
                $"Image base64 length {image.ImageData.Length} exceeds max {maxBase64Length}. " +
                "Compression should have reduced it.");

            var sizeKb = (image.ImageData.Length * 3) / 4 / 1024;
            Console.WriteLine($"   ✅ Image: {image.MimeType}, base64 length: {image.ImageData.Length}, ~{sizeKb} KB — within limit");
        }
    }

    [TestMethod]
    public void Lookup_MultiplePagesReturnDifferentContent()
    {
        // Act
        var page1 = _documentLookup.Lookup(TestDocumentId, page: 1);
        var page2 = _documentLookup.Lookup(TestDocumentId, page: 2);

        // Assert
        Assert.IsNotNull(page1, "Page 1 result should not be null");
        Assert.IsNotNull(page2, "Page 2 result should not be null");
        Assert.AreNotEqual(page1.Text, page2.Text,
            "Page 1 and Page 2 should have different text content");

        Console.WriteLine($"✅ Page 1: {page1.Text.Length} chars, Page 2: {page2.Text.Length} chars");
    }

    [TestMethod]
    public void Lookup_ThrowsOnInvalidDocument()
    {
        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() =>
            _documentLookup.Lookup("nonexistent_document.pdf", page: 1),
            "Should throw when document does not exist");

        Console.WriteLine("✅ Correctly throws for nonexistent document");
    }

    [TestMethod]
    public void Lookup_ThrowsOnInvalidPage()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            _documentLookup.Lookup(TestDocumentId, page: 99999),
            "Should throw when page number is out of range");

        Console.WriteLine("✅ Correctly throws for out-of-range page number");
    }

    /// <summary>
    /// Minimal mock of IWebHostEnvironment for testing DocumentLookup
    /// </summary>
    private class MockWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "TestApplication";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
