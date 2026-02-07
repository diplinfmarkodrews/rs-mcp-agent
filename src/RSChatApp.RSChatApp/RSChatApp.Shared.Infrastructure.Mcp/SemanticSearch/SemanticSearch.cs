using Microsoft.Extensions.VectorData;
using RSChatApp.Shared.Infrastructure.Mcp.Ingestion.Models;

namespace RSChatApp.Shared.Infrastructure.Mcp.SemanticSearch;

public class SemanticSearch(
    VectorStoreCollection<Guid, IngestedChunk> vectorCollection) : ISemanticSearch
{
    public async Task<IReadOnlyList<IngestedChunk>> SearchAsync(string text, string? documentIdFilter, int maxResults)
    {
        var nearest = vectorCollection.SearchAsync(text, maxResults, new VectorSearchOptions<IngestedChunk>
        {
            Filter = documentIdFilter is { Length: > 0 } ? record => record.DocumentId == documentIdFilter : null,
        });

        return await nearest.Select(result => result.Record).ToListAsync();
    }
}
