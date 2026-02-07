namespace RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Services;

public interface IStaticContentFileStore
{
    Task<string?> GetTextAsync(string sourceName, string relativePath, CancellationToken cancellationToken = default);
    Task<byte[]?> GetBytesAsync(string sourceName, string relativePath, CancellationToken cancellationToken = default);
}
