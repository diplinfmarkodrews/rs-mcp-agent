using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RSChatApp.Mcp.Browser.Configuration;
using RSChatApp.Mcp.Browser.Interfaces;
using RSChatApp.Mcp.Browser.Extensions;
using System.Collections.Concurrent;
using LazyCache;

namespace RSChatApp.Mcp.Browser.Infrastructure;

public class InMemoryBrowserInstanceStore : IBrowserInstanceStore
{
    private readonly ILogger<InMemoryBrowserInstanceStore> _logger;
    private readonly IBrowserInstanceFactory _browserInstanceFactory;
    private readonly IAppCache _memoryCache;
    private readonly ConcurrentDictionary<string, DisconnectedEventHandler> _eventHandlers = new();

    public InMemoryBrowserInstanceStore(ILogger<InMemoryBrowserInstanceStore> logger, 
        IAppCache memoryCache,
        IBrowserInstanceFactory browserInstanceFactory)
    {
        _logger = logger;
        _memoryCache = memoryCache;
        _browserInstanceFactory = browserInstanceFactory;
    }
    
    public async Task<IBrowserInstance> GetOrCreateBrowserInstanceAsync(string sessionId, BrowserInstanceConfiguration? config = null)
    {
        return await _memoryCache.GetOrAddAsync(
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
                    entry.SlidingExpiration = TimeSpan.FromMinutes(config?.SlidingExpirationMinutes ?? 30); 
                    var instance = await _browserInstanceFactory.CreateInstanceAsync(config);
                    // Store event handler for proper unsubscription
                    DisconnectedEventHandler eventHandler = () => BrowserOnDisconnected(instance);
                    _eventHandlers[sessionId] = eventHandler;
                    instance.Disconnected += eventHandler;
                    return instance;
                });
    }
    

    public async Task RemoveInstanceAsync(string sessionId)
    {
        var sessionKey = sessionId.CreatBrowserInstanceCacheKey();
        _memoryCache.TryGetValue<IBrowserInstance>(sessionKey, out var instance );
        if (instance != null && instance is IBrowserInstance browserInstance)
        {
            _memoryCache.Remove(sessionKey);
            
            // Remove the stored event handler
            if (_eventHandlers.TryRemove(sessionId, out var eventHandler))
            {
                browserInstance.Disconnected -= eventHandler;
            }
            
            await browserInstance.DisposeAsync();
            _logger.LogInformation("Disposed browser instance for session {SessionId}", sessionId);
        }
        else
        {
            _logger.LogWarning("No browser instance found for session {SessionId} to dispose", sessionId);
        }
    }

    private Task BrowserOnDisconnected(IBrowserInstance browserInstance)
    {
        var sessionId = browserInstance.SessionId;
        _logger.LogInformation("Browser instance disconnected for session {SessionId}. Cleaning up cache entry.", sessionId);
        _memoryCache.Remove(sessionId.CreatBrowserInstanceCacheKey());
        return Task.CompletedTask;
    }
}