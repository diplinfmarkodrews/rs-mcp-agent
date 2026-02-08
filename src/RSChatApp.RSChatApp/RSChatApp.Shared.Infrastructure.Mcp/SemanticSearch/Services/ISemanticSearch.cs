using RSChatApp.Shared.Infrastructure.Mcp.Ingestion.Models;

namespace RSChatApp.Shared.Infrastructure.Mcp.SemanticSearch.Services;

public interface ISemanticSearch
{
    Task<IReadOnlyList<IngestedChunk>> SearchAsync(string text, string? documentIdFilter, int maxResults);
}