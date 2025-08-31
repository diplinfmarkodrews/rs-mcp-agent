using Microsoft.Playwright;

namespace RSChatApp.Mcp.Browser.Core;

public interface IBrowserInstance : IAsyncDisposable
{
    // only for development
    IBrowser Browser { get; }       // Damn, PlayWright dependencies shall not be used directly
    IBrowserContext BrowserContext { get; set; }
    IPage Page { get; set; }
    //
    Task LoginAsync(string username, string password);
    Task NewContextAsync();
}