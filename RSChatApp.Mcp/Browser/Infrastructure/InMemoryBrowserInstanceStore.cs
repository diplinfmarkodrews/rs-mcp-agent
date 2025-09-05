using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using RSChatApp.Mcp.Browser.Configuration;
using RSChatApp.Mcp.Browser.Interfaces;
using RSChatApp.Mcp.Browser.Extensions;

namespace RSChatApp.Mcp.Browser.Infrastructure;

public class InMemoryBrowserInstanceStore : IBrowserInstanceStore
{
    private readonly ILogger<InMemoryBrowserInstanceStore> _logger;
    private readonly IBrowserInstanceFactory _browserInstanceFactory;
    private readonly IMemoryCache _memoryCache;

    public InMemoryBrowserInstanceStore(ILogger<InMemoryBrowserInstanceStore> logger, 
        IMemoryCache memoryCache,
        IBrowserInstanceFactory browserInstanceFactory)
    {
        _logger = logger;
        _memoryCache = memoryCache;
        _browserInstanceFactory = browserInstanceFactory;
    }
    
    public async Task<IBrowserInstance> GetOrCreateBrowserInstanceAsync(string sessionId, BrowserInstanceConfiguration config = null)
    {
        return await _memoryCache.GetOrCreateAsync(
            sessionId.CreatBrowserInstanceCacheKey(), 
            async (entry) =>
                {
                    entry.PostEvictionCallbacks.Add(    
                        new PostEvictionCallbackRegistration
                        {
                            EvictionCallback = async (key, value, reason, state) =>
                            {
                                if (value is IBrowserInstance browserInstance)
                                {
                                    _logger.LogInformation("Evicting browser instance for session {SessionId} due to {Reason}", sessionId, reason);
                                    await browserInstance.DisposeAsync();
                                }
                            }
                        });
                    var instance = await _browserInstanceFactory.CreateInstanceAsync(config);
                    instance.Disconnected += BrowserOnDisconnected;
                    return instance;
                });
    }
    

    public async Task DisposeInstanceAsync(string sessionId)
    {
        var sessionKey = sessionId.CreatBrowserInstanceCacheKey();
        _memoryCache.TryGetValue(sessionKey, out var instance );
        if (instance != null && instance is IBrowserInstance browserInstance)
        {
            _memoryCache.Remove(sessionKey);
            browserInstance.Disconnected -= BrowserOnDisconnected;
            await browserInstance.DisposeAsync();
            _logger.LogInformation("Disposed browser instance for session {SessionId}", sessionId);
        }
        else
        {
            _logger.LogWarning("No browser instance found for session {SessionId} to dispose", sessionId);
        }
    }

    private void BrowserOnDisconnected(object? sender, IBrowserInstance e)
    {
        _logger.LogInformation("Browser instance disconnected for session {SessionId}. Clear cacheEntry", e.SessionId);
        _memoryCache.Remove(e.SessionId.CreatBrowserInstanceCacheKey());
    }
}