using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Configuration;
using RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Models;

namespace RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Services;

public sealed class StaticContentIndexStore : IStaticContentIndexStore, IDisposable
{
    private readonly ILogger<StaticContentIndexStore> _logger;
    private readonly StaticContentOptions _options;

    private readonly Dictionary<string, SourceState> _sources = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public StaticContentIndexStore(
        IOptions<StaticContentOptions> options,
        IWebHostEnvironment env,
        ILogger<StaticContentIndexStore> logger)
    {
        _logger = logger;
        _options = options.Value ?? new StaticContentOptions();

        foreach (var src in _options.Sources)
        {
            if (string.IsNullOrWhiteSpace(src.Name) || string.IsNullOrWhiteSpace(src.Path))
            {
                continue;
            }

            var physicalPath = ResolvePath(src.Path, env.ContentRootPath);
            if (!Directory.Exists(physicalPath))
            {
                _logger.LogWarning("Static content source {Name} path not found: {Path}", src.Name, physicalPath);
                continue;
            }

            var provider = new PhysicalFileProvider(physicalPath);
            var include = NormalizeExtensions(src.IncludeExtensions);
            _sources[src.Name] = new SourceState(src.Name, physicalPath, provider, include);
        }

        // Build index eagerly so first queries are fast.
        foreach (var source in _sources.Values)
        {
            RebuildIndex(source);
        }
    }

    public IReadOnlyList<StaticContentItem> GetAll(string sourceName)
    {
        if (!_sources.TryGetValue(sourceName, out var src))
        {
            return Array.Empty<StaticContentItem>();
        }

        lock (_gate)
        {
            return src.Items;
        }
    }

    public IEnumerable<StaticContentItem> Query(string sourceName, StaticContentQuery query)
    {
        var items = GetAll(sourceName);
        if (items.Count == 0)
        {
            return items;
        }

        IEnumerable<StaticContentItem> result = items;

        if (!string.IsNullOrWhiteSpace(query.Prefix))
        {
            var prefix = NormalizeRelativePath(query.Prefix);
            result = result.Where(i => i.RelativePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Contains))
        {
            var contains = query.Contains.Trim();
            result = result.Where(i => i.RelativePath.Contains(contains, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Extension))
        {
            var ext = query.Extension.StartsWith('.') ? query.Extension : "." + query.Extension;
            result = result.Where(i => string.Equals(i.Extension, ext, StringComparison.OrdinalIgnoreCase));
        }

        if (query.ContentType.HasValue)
        {
            result = result.Where(i => i.ContentType == query.ContentType.Value);
        }
        var limit = query.Limit.GetValueOrDefault(200);
        if (limit > 0)
        {
            result = result.Take(limit);
        }

        return result;
    }

    public bool TryGet(string sourceName, string relativePath, out StaticContentItem item)
    {
        item = default!;
        var normalized = NormalizeRelativePath(relativePath);
        var items = GetAll(sourceName);
        if (items.Count == 0)
        {
            return false;
        }

        var found = items.FirstOrDefault(i => string.Equals(i.RelativePath, normalized, StringComparison.OrdinalIgnoreCase));
        if (found is null)
        {
            return false;
        }

        item = found;
        return true;
    }

    private void RebuildIndex(SourceState source)
    {
        try
        {
            var items = new List<StaticContentItem>(capacity: 10_000);
            EnumerateRecursive(
                source.Name,
                source.Provider,
                subpath: string.Empty,
                items,
                includeExtensions: source.IncludeExtensions);
            items.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase));

            lock (_gate)
            {
                source.Items = items;
            }

            _logger.LogInformation(
                "Indexed static content source {Name}: {Count} files from {Path}",
                source.Name,
                items.Count,
                source.PhysicalPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index static content source {Name}", source.Name);
        }
    }

    private static void EnumerateRecursive(
        string sourceName,
        IFileProvider provider,
        string subpath,
        List<StaticContentItem> items,
        IReadOnlySet<string> includeExtensions)
    {
        var contents = provider.GetDirectoryContents(subpath);
        if (!contents.Exists)
        {
            return;
        }

        foreach (var entry in contents)
        {
            if (entry.IsDirectory)
            {
                var next = string.IsNullOrEmpty(subpath) ? entry.Name : $"{subpath}/{entry.Name}";
                EnumerateRecursive(sourceName, provider, next, items, includeExtensions);
                continue;
            }

            var rel = string.IsNullOrEmpty(subpath) ? entry.Name : $"{subpath}/{entry.Name}";
            rel = NormalizeRelativePath(rel);

            var ext = NormalizeExtension(Path.GetExtension(entry.Name));

            if (includeExtensions.Count > 0)
            {
                // If a file has no extension and we have an allow-list, it is excluded.
                if (string.IsNullOrWhiteSpace(ext) || !includeExtensions.Contains(ext))
                {
                    continue;
                }
            }

            items.Add(new StaticContentItem
            {
                SourceName = sourceName,
                RelativePath = rel,
                Length = entry.Length,
                LastModified = entry.LastModified.LocalDateTime,
                Extension = string.IsNullOrWhiteSpace(ext) ? null : ext
            });
        }
    }

    private static IReadOnlySet<string> NormalizeExtensions(List<string>? extensions)
    {
        if (extensions is null || extensions.Count == 0)
        {
            return EmptyExtensions;
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in extensions)
        {
            var n = NormalizeExtension(e);
            if (!string.IsNullOrWhiteSpace(n))
            {
                set.Add(n);
            }
        }

        return set.Count == 0 ? EmptyExtensions : set;
    }

    private static string? NormalizeExtension(string? ext)
    {
        if (string.IsNullOrWhiteSpace(ext))
        {
            return null;
        }

        var e = ext.Trim();
        if (!e.StartsWith(".", StringComparison.Ordinal))
        {
            e = "." + e;
        }

        return e;
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

        // Disallow traversal.
        if (p.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Relative path cannot contain '..'.");
        }

        return p;
    }

    public void Dispose()
    {
        foreach (var src in _sources.Values)
        {
            if (src.Provider is IDisposable d)
            {
                d.Dispose();
            }
        }
    }

    private sealed class SourceState
    {
        public SourceState(
            string name,
            string physicalPath,
            IFileProvider provider,
            IReadOnlySet<string> includeExtensions)
        {
            Name = name;
            PhysicalPath = physicalPath;
            Provider = provider;
            IncludeExtensions = includeExtensions;
        }

        public string Name { get; }
        public string PhysicalPath { get; }
        public IFileProvider Provider { get; }
        public List<StaticContentItem> Items { get; set; } = new();
        public IReadOnlySet<string> IncludeExtensions { get; }
    }

    private static readonly IReadOnlySet<string> EmptyExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
