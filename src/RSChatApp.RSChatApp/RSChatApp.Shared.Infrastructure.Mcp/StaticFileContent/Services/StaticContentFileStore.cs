using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Configuration;

namespace RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Services;

public sealed class StaticContentFileStore : IStaticContentFileStore, IDisposable
{
    private readonly ILogger<StaticContentFileStore> _logger;
    private readonly IMemoryCache _cache;
    private readonly Dictionary<string, IFileProvider> _providers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _roots = new(StringComparer.OrdinalIgnoreCase);

    public StaticContentFileStore(
        IOptions<StaticContentOptions> options,
        IWebHostEnvironment env,
        IMemoryCache cache,
        ILogger<StaticContentFileStore> logger)
    {
        _logger = logger;
        _cache = cache;

        var cfg = options.Value ?? new StaticContentOptions();
        foreach (var src in cfg.Sources)
        {
            if (string.IsNullOrWhiteSpace(src.Name) || string.IsNullOrWhiteSpace(src.Path))
            {
                continue;
            }

            var physicalPath = ResolvePath(src.Path, env.ContentRootPath);
            if (!Directory.Exists(physicalPath))
            {
                continue;
            }

            _providers[src.Name] = new PhysicalFileProvider(physicalPath);
            _roots[src.Name] = physicalPath;
        }
    }

    public async Task<string?> GetTextAsync(string sourceName, string relativePath, CancellationToken cancellationToken = default)
    {
        var bytes = await GetBytesAsync(sourceName, relativePath, cancellationToken).ConfigureAwait(false);
        if (bytes is null)
        {
            return null;
        }

        return Encoding.UTF8.GetString(bytes);
    }

    public async Task<byte[]?> GetBytesAsync(string sourceName, string relativePath, CancellationToken cancellationToken = default)
    {
        if (!_providers.TryGetValue(sourceName, out var provider))
        {
            return null;
        }

        var rel = NormalizeRelativePath(relativePath);
        var cacheKey = $"StaticContent:{sourceName}:{rel}";

        if (_cache.TryGetValue(cacheKey, out byte[]? cached) && cached is not null)
        {
            return cached;
        }

        var fileInfo = provider.GetFileInfo(rel);
        if (!fileInfo.Exists || fileInfo.IsDirectory)
        {
            return null;
        }

        try
        {
            await using var stream = fileInfo.CreateReadStream();
            using var ms = new MemoryStream(capacity: (int)Math.Min(fileInfo.Length, int.MaxValue));
            await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            var bytes = ms.ToArray();

            // Cache and invalidate when the underlying file changes.
            var token = provider.Watch(rel);
            _cache.Set(cacheKey, bytes, new MemoryCacheEntryOptions()
                .AddExpirationToken(token));

            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read static content file {Source}:{Path}", sourceName, rel);
            return null;
        }
    }

    private static string ResolvePath(string path, string contentRoot)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        return Path.GetFullPath(Path.Combine(contentRoot, path));
    }

    private static string NormalizeRelativePath(string path)
    {
        var p = path.Replace('\\', '/').Trim();
        while (p.StartsWith("/", StringComparison.Ordinal))
        {
            p = p[1..];
        }

        if (p.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Relative path cannot contain '..'.");
        }

        return p;
    }

    public void Dispose()
    {
        foreach (var p in _providers.Values)
        {
            if (p is IDisposable d)
            {
                d.Dispose();
            }
        }
    }
}
