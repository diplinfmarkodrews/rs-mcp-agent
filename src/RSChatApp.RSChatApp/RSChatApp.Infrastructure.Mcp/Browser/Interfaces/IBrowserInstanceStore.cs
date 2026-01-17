using RSChatApp.Mcp.Browser.Configuration;

namespace RSChatApp.Mcp.Browser.Interfaces;

public interface IBrowserInstanceStore
{
    Task<IBrowserInstance> GetOrCreateBrowserInstanceAsync(string sessionId, BrowserInstanceConfiguration config = null);
    
    Task RemoveInstanceAsync(string sessionId);
}

