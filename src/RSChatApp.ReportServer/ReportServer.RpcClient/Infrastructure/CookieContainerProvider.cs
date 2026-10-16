using System.Net;
using Microsoft.Extensions.Caching.Memory;

namespace ReportServer.RpcClient.Infrastructure;
public class CookieContainerProvider
{
    // Idle ReportServer sessions of bearer-token callers decay after this time.
    private static readonly TimeSpan BearerSessionIdleTimeout = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;
    public CookieContainer CookieContainer { get => _cookieContainer; }
    private CookieContainer _cookieContainer = new CookieContainer();

    public CookieContainerProvider(IMemoryCache cache) => _cache = cache;

    public void ClearCookies()
    {
        _cookieContainer = new CookieContainer();
    }

    public string? GetBearerSession(string key) => _cache.Get<string>(BearerSessionKey(key));

    public void SetBearerSession(string key, string jsessionId) =>
        _cache.Set(BearerSessionKey(key), jsessionId, new MemoryCacheEntryOptions { SlidingExpiration = BearerSessionIdleTimeout });

    private static string BearerSessionKey(string key) => $"rs-bearer-session:{key}";
}
