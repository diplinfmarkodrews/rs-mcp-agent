using RSChatApp.Mcp.Browser.Configuration;

namespace RSChatApp.Mcp.Browser.Core;

public interface IBrowserInstanceFactory
{
    Task<IBrowserInstance> CreateAsync(BrowserInstanceConfiguration instanceConfiguration = null);
}