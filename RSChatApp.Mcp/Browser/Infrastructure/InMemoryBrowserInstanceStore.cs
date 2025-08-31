using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RSChatApp.Mcp.Browser.Configuration;
using RSChatApp.Mcp.Browser.Core;
using RSChatApp.Mcp.Browser.Extensions;

namespace RSChatApp.Mcp.Browser.Infrastructure;

public class InMemoryBrowserInstanceStore : IBrowserInstanceStore
{
    
    //TODO: refactor to IMemoryCache with expiration
    private readonly ConcurrentDictionary<string, IBrowserInstance> _browserInstances = new ();
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
                    // TODO: set callback to DisposeAsync
                    return await _browserInstanceFactory.CreateAsync(config); // can be null
                });
    }
    

    public async Task DisposeInstanceAsync(string sessionId)
    {
        var sessionKey = sessionId.CreatBrowserInstanceCacheKey();
        _memoryCache.TryGetValue(sessionKey, out var instance );
        if (instance != null && instance is IBrowserInstance browserInstance)
        {
            _memoryCache.Remove(sessionKey);
            await browserInstance.DisposeAsync();
            _logger.LogInformation("Disposed browser instance for session {SessionId}", sessionId);
        }
        else
        {
            _logger.LogWarning("No browser instance found for session {SessionId} to dispose", sessionId);
        }
    }
}