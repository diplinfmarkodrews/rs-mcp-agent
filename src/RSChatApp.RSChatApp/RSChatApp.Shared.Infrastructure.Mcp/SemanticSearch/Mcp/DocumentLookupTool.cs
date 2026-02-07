using System.ComponentModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using UglyToad.PdfPig;

namespace RSChatApp.Shared.Infrastructure.Mcp.SemanticSearch.Mcp;
/// <summary>
/// Document lookup to load single document pages, optionally with images
/// </summary>
public class DocumentLookupTool
{
    private readonly DocumentLookup _documentLookup;

    public DocumentLookupTool(DocumentLookup documentLookup)
    {
        _documentLookup = documentLookup;
    }
    [KernelFunction, McpServerTool,  Description("Retrieves a single page of a given document, optionally attaches all images on that page")]
    public async Task<object> GetDocumentPage(
        [Description("Document name to reference the document")] string documentId, 
        [Description("Page of the document to retrieve")] int page, 
        [Description("Optionally add images")] bool addImages = false)
    {
        
        try
        {
            return _documentLookup.Lookup(documentId, page, addImages);
        }
        catch
        {

            return "Error: Document not found";
        }
    } 
}

public record DocumentLookupResult(
    string DocumentId,
    string Text,
    DocumentLookupImageResult[]? Images,
    DateTime Timestamp 
);

public record DocumentLookupImageResult(string ImageData);
