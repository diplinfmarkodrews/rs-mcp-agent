using LazyCache;
using Microsoft.AspNetCore.Http;
using RSChatApp.Mcp.Browser.Extensions;
using RSChatApp.Mcp.Browser.Interfaces;

namespace RSChatApp.Mcp.Browser.Implementations;

public class BrowserInstanceProvider : IBrowserInstanceProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    private readonly IAppCache _memoryCache;

    public BrowserInstanceProvider(IHttpContextAccessor httpContextAccessor, 
        IAppCache memoryCache)
    {
        _httpContextAccessor = httpContextAccessor;
        _memoryCache = memoryCache;
    }
    
    public IBrowserInstance GetBrowserInstance()
    {
        var sessionId = _httpContextAccessor.HttpContext.Session.Id;
        if (string.IsNullOrEmpty(sessionId))
            throw new InvalidOperationException("SessionId is null, HttpContext already disposed?!");
        
        var browserCacheKey = sessionId.CreatBrowserInstanceCacheKey();
        
        if (_memoryCache.CacheProvider.TryGetValue(browserCacheKey, out AsyncLazy<IBrowserInstance> memoryCacheEntry) == false)
            throw new InvalidOperationException("SessionId not found in MemoryCache, BrowserInstance not created yet?");
        
        if (memoryCacheEntry is AsyncLazy<IBrowserInstance> browserInstance)
            return browserInstance.GetAwaiter().GetResult();
        
        throw new InvalidDataException($"BrowserCacheEntry is not IBrowserInstance, type: {memoryCacheEntry.GetType().FullName}");
    }
    // public async Task<IBrowserInstance> GetBrowserInstanceAsync()
    // {
    //     string sessionId = _httpContextAccessor.HttpContext.Session.Id;
    //     if (string.IsNullOrEmpty(sessionId))
    //         throw new InvalidOperationException("SessionId is null, HttpContext already disposed?!");
    //     
    //     var browserInstance = await _browserStore.GetOrCreateBrowserInstanceAsync(sessionId);
    //     if (browserInstance == null)
    //         throw new InvalidOperationException("SessionId not found in BrowserInstanceStore");
    //     
    //     return browserInstance;
    // }
    // public async Task<IBrowserInstance> GetBrowserInstanceAsync(string sessionId)
    // {
    //     var browserInstance = await _browserStore.GetOrCreateBrowserInstanceAsync(sessionId);
    //     if (browserInstance == null)
    //         throw new InvalidOperationException("SessionId not found in BrowserInstanceStore");
    //     
    //     return browserInstance;
    // }
}
