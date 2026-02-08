using RSChatApp.Shared.Infrastructure.Mcp.Browser.Configuration;

namespace RSChatApp.Shared.Infrastructure.Mcp.Browser.Interfaces;

public interface IBrowserInstanceStore
{
    Task<IBrowserInstance> GetOrCreateBrowserInstanceAsync(string sessionId, BrowserInstanceConfiguration config = null);
    
    Task RemoveInstanceAsync(string sessionId);
}

