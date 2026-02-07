using RSChatApp.Shared.Infrastructure.Mcp.Browser.Configuration;

namespace RSChatApp.Shared.Infrastructure.Mcp.Browser.Interfaces;

public interface IBrowserInstanceFactory 
{
    Task<IBrowserInstance> CreateInstanceAsync(BrowserInstanceConfiguration instanceConfiguration = null);
}