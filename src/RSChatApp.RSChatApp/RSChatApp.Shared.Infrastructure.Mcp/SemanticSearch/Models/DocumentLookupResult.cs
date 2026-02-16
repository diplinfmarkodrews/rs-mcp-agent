namespace RSChatApp.Shared.Infrastructure.Mcp.SemanticSearch.Models;

public record DocumentLookupResult(
    string DocumentId,
    string Text,
    DocumentLookupImageResult[]? Images,
    DateTime Timestamp 
);

public record DocumentLookupImageResult(string ImageData, string MimeType = "image/png");