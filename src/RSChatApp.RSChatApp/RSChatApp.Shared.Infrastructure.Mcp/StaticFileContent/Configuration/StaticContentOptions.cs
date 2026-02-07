namespace RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Configuration;

public sealed class StaticContentOptions
{
    public List<StaticContentSourceOptions> Sources { get; init; } = new();
}

public sealed class StaticContentSourceOptions
{
    /// <summary>Logical name of the source, used by the index store.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Physical root directory. Can be absolute or relative to the application's content root.
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Public request path (e.g. "/rs-scripts"). If empty, the source is not exposed via static files.
    /// </summary>
    public string? RequestPath { get; init; }

    /// <summary>
    /// Cache-Control max-age in seconds for static file responses.
    /// </summary>
    public int CacheMaxAgeSeconds { get; init; } = 3600;

    /// <summary>
    /// Optional allow-list of file extensions to index (e.g. [".groovy", "md"]).
    /// If empty, all files are indexed.
    /// </summary>
    public List<string> IncludeExtensions { get; init; } = new();
}
