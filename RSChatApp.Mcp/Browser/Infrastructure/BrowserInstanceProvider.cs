using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using RSChatApp.Mcp.Browser.Extensions;

namespace RSChatApp.Mcp.Browser.Interfaces;

public class BrowserInstanceProvider : IBrowserInstanceProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IBrowserInstanceStore _browserStore;
    private readonly IMemoryCache _memoryCache;

    public BrowserInstanceProvider(IHttpContextAccessor httpContextAccessor, 
        IMemoryCache memoryCache,
        IBrowserInstanceStore browserStore)
    {
        _httpContextAccessor = httpContextAccessor;
        _browserStore = browserStore;
        _memoryCache = memoryCache;
    }
    
    public IBrowserInstance GetBrowserInstance()
    {
        var sessionId = _httpContextAccessor.HttpContext.Session.Id;
        if (string.IsNullOrEmpty(sessionId))
            throw new InvalidOperationException("SessionId is null, HttpContext already disposed?!");
        
        var browserCacheKey = sessionId.CreatBrowserInstanceCacheKey();
        if (_memoryCache.TryGetValue(browserCacheKey, out var memoryCacheEntry)==false
            || memoryCacheEntry == null)
            throw new InvalidOperationException("SessionId not found in MemoryCache, BrowserInstance not created yet?");
        
        if (memoryCacheEntry is IBrowserInstance browserInstance)
            return browserInstance;
        
        throw new InvalidDataException($"BrowserCacheEntry is not IBrowserInstance, type: {memoryCacheEntry.GetType().FullName}");
    }
    public async Task<IBrowserInstance> GetBrowserInstanceAsync()
    {
        string sessionId = _httpContextAccessor.HttpContext.Session.Id;
        if (string.IsNullOrEmpty(sessionId))
            throw new InvalidOperationException("SessionId is null, HttpContext already disposed?!");
        
        var browserInstance = await _browserStore.GetOrCreateBrowserInstanceAsync(sessionId);
        if (browserInstance == null)
            throw new InvalidOperationException("SessionId not found in BrowserInstanceStore");
        
        return browserInstance;
    }
    public async Task<IBrowserInstance> GetBrowserInstanceAsync(string sessionId)
    {
        var browserInstance = await _browserStore.GetOrCreateBrowserInstanceAsync(sessionId);
        if (browserInstance == null)
            throw new InvalidOperationException("SessionId not found in BrowserInstanceStore");
        
        return browserInstance;
    }
}
