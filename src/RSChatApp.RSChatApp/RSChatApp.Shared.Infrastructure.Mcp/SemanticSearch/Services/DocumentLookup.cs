using Microsoft.AspNetCore.Hosting;
using RSChatApp.Shared.Infrastructure.Mcp.SemanticSearch.Models;
using SkiaSharp;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;

namespace RSChatApp.Shared.Infrastructure.Mcp.SemanticSearch.Services;

/// <summary>
/// Controls how text is extracted from PDF pages.
/// </summary>
public enum TextExtractionMode
{
    /// <summary>
    /// Raw content-order text from the PDF stream (Page.Text). Fast but no formatting.
    /// </summary>
    Raw,

    /// <summary>
    /// Uses DocumentLayoutAnalysis with RenderingReadingOrderDetector.
    /// Orders blocks by PDF rendering sequence (TextSequence). Good for simple, single-column PDFs.
    /// </summary>
    RenderingOrder,

    /// <summary>
    /// Uses DocumentLayoutAnalysis with UnsupervisedReadingOrderDetector.
    /// Infers reading order from spatial reasoning (Allen's interval relations). Best for complex layouts.
    /// </summary>
    SpatialOrder
}

public interface IDocumentLookup
{
    DocumentLookupResult Lookup(string documentId, int page, bool addImages = false,
        TextExtractionMode textMode = TextExtractionMode.RenderingOrder);
}
public class DocumentLookup : IDocumentLookup
{
    private readonly string _sourceDirectory;
    public DocumentLookup(IWebHostEnvironment env)
    {
        _sourceDirectory = Path.Combine(env.WebRootPath, "Data");
    }
    
    public DocumentLookupResult Lookup(
        string documentId, int page, bool addImages = false,
        TextExtractionMode textMode = TextExtractionMode.RenderingOrder)
    {
        string documentPath = Path.Combine(_sourceDirectory, documentId);
        PdfDocument? document; 
        document = PdfDocument.Open(documentPath);

        var documentPage = document.GetPage(page);
        var text = ExtractText(documentPage, textMode);
        List<DocumentLookupImageResult> images = new ();
        if (addImages && documentPage.NumberOfImages > 0)
        {
            foreach (var image in documentPage.GetImages())
            {
                byte[]? imageBytes = null;
                string mimeType = "image/png";

                if (image.TryGetPng(out var pngBytes))
                {
                    imageBytes = pngBytes;
                    mimeType = "image/png";
                }
                else
                {
                    // TryGetPng doesn't support JPEG — detect JPEG magic bytes
                    var raw = image.RawBytes;
                    if (raw is { Length: > 2 } && raw[0] == 0xFF && raw[1] == 0xD8)
                    {
                        imageBytes = raw.ToArray();
                        mimeType = "image/jpeg";
                    }
                    // Skip images in unsupported formats (JPEG2000, CCITT, etc.)
                }

                if (imageBytes is not null)
                {
                    var (compressedBytes, compressedMime) = CompressImageIfNeeded(imageBytes, mimeType);
                    images.Add(new DocumentLookupImageResult(
                        Convert.ToBase64String(compressedBytes), compressedMime));
                }
            }
        }
        
        return new DocumentLookupResult(
            documentId, 
            text, 
            images.Count > 0 ? images.ToArray() : null, 
            DateTime.Now);
    }

    private static string ExtractText(Page page, TextExtractionMode mode)
    {
        if (mode == TextExtractionMode.Raw)
            return page.Text;

        var words = page.GetWords();
        if (!words.Any())
            return page.Text;

        var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);
        if (blocks.Count == 0)
            return page.Text;

        IReadingOrderDetector detector = mode switch
        {
            TextExtractionMode.SpatialOrder => UnsupervisedReadingOrderDetector.Instance,
            _ => RenderingReadingOrderDetector.Instance
        };

        var ordered = detector.Get(blocks);
        return string.Join("\n\n", ordered.Select(b => b.Text));
    }

    private const int MaxBase64Length = 512_000; // ~500 KB
    private const int MaxDimension = 1200;

    private static (byte[] Bytes, string MimeType) CompressImageIfNeeded(byte[] imageBytes, string mimeType)
    {
        // Estimate base64 length without allocating
        var base64Len = ((imageBytes.Length + 2) / 3) * 4;
        if (base64Len <= MaxBase64Length)
        {
            return (imageBytes, mimeType);
        }

        using var bitmap = SKBitmap.Decode(imageBytes);
        if (bitmap is null)
        {
            return (imageBytes, mimeType);
        }

        var targetBitmap = bitmap;
        try
        {
            // Resize if either dimension exceeds MaxDimension
            if (bitmap.Width > MaxDimension || bitmap.Height > MaxDimension)
            {
                var scale = Math.Min((float)MaxDimension / bitmap.Width, (float)MaxDimension / bitmap.Height);
                var newWidth = (int)(bitmap.Width * scale);
                var newHeight = (int)(bitmap.Height * scale);
                targetBitmap = bitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.Medium);
            }

            // Encode as JPEG at quality 75
            using var skImage = SKImage.FromBitmap(targetBitmap);
            using var data = skImage.Encode(SKEncodedImageFormat.Jpeg, 75);
            var result = data.ToArray();

            // If still too large, try quality 50
            var resultBase64Len = ((result.Length + 2) / 3) * 4;
            if (resultBase64Len > MaxBase64Length)
            {
                using var data2 = skImage.Encode(SKEncodedImageFormat.Jpeg, 50);
                result = data2.ToArray();
            }

            return (result, "image/jpeg");
        }
        finally
        {
            if (targetBitmap != bitmap)
            {
                targetBitmap.Dispose();
            }
        }
    }
}