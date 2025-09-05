using Microsoft.Playwright;

namespace RSChatApp.Mcp.Browser.Interfaces;

public interface IBrowserInstance : IAsyncDisposable
{
    // only for development
    IBrowser Browser { get; }       // Damn, PlayWright dependencies shall not be used directly
    IBrowserContext BrowserContext { get; set; }
    IPage Page { get; set; }
    public abstract event EventHandler<IBrowserInstance> Disconnected;
    public string SessionId { get; }
    Task LoginAsync(string username, string password);
    Task NewContextAsync();
}