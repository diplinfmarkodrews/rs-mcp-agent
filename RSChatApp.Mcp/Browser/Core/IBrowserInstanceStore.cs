using RSChatApp.Mcp.Browser.Configuration;

namespace RSChatApp.Mcp.Browser.Core;

public interface IBrowserInstanceStore
{
    Task<IBrowserInstance> GetOrCreateBrowserInstanceAsync(string sessionId, BrowserInstanceConfiguration config = null);
    
    Task DisposeInstanceAsync(string sessionId);
}

