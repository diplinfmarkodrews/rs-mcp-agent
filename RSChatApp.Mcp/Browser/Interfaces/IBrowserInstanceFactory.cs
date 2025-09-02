using RSChatApp.Mcp.Browser.Configuration;

namespace RSChatApp.Mcp.Browser.Interfaces;

public interface IBrowserInstanceFactory 
{
    Task<IBrowserInstance> CreateInstanceAsync(BrowserInstanceConfiguration instanceConfiguration = null);
}