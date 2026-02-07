using Microsoft.AspNetCore.Hosting;
using RSChatApp.Shared.Infrastructure.Mcp.SemanticSearch.Mcp;
using UglyToad.PdfPig;

namespace RSChatApp.Shared.Infrastructure.Mcp.SemanticSearch;


public interface IDocumentLookup
{
    DocumentLookupResult Lookup(string documentId, int page,  bool addImages = false);
}
public class DocumentLookup 
{
    private readonly string _sourceDirectory;
    public DocumentLookup(IWebHostEnvironment env)
    {
        _sourceDirectory = Path.Combine(env.WebRootPath, "Data");
    }
    public DocumentLookupResult Lookup(string documentId, int page, bool  addImages = false){
        string documentPath = Path.Combine(_sourceDirectory, documentId);
        PdfDocument? document; 
        document = PdfDocument.Open(documentPath);

        var documentPage = document.GetPage(page);
        List<DocumentLookupImageResult> images = new ();
        if (addImages && documentPage.NumberOfImages > 0)
        {
            foreach (var image in documentPage.GetImages())
            {
                var base64Image = Convert.ToBase64String(image.RawBytes);
                images.Add(new DocumentLookupImageResult(base64Image));
            }
        }
        
        return new DocumentLookupResult(
            documentId, 
            documentPage.Text, 
            images.Count > 0 ? images.ToArray() : null, 
            DateTime.Now);
    } 
}