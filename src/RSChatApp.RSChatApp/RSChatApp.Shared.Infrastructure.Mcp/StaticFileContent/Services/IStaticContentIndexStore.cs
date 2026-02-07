using RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Models;

namespace RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Services;

public interface IStaticContentIndexStore
{
    IReadOnlyList<StaticContentItem> GetAll(string sourceName);
    IEnumerable<StaticContentItem> Query(string sourceName, StaticContentQuery query);
    bool TryGet(string sourceName, string relativePath, out StaticContentItem item);
}
