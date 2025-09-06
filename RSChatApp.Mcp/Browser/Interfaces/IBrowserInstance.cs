using Microsoft.Playwright;

namespace RSChatApp.Mcp.Browser.Interfaces;

// Typed event delegates
public delegate Task NavigationStartedEventHandler(string url);
public delegate Task NavigationCompletedEventHandler(string url, string title);
public delegate Task NavigationFailedEventHandler(string url, string error);
public delegate Task UrlChangedEventHandler(string newUrl);
public delegate Task TitleChangedEventHandler(string newTitle);
public delegate Task LoadingStateChangedEventHandler(bool isLoading);
public delegate Task PageErrorEventHandler(string message, string source);
public delegate Task DisconnectedEventHandler();

public interface IBrowserInstance : IAsyncDisposable
{
    // Development only (TODO: Remove Playwright dependencies)
    IBrowser Browser { get; }
    IBrowserContext BrowserContext { get; set; }
    // IPage Page { get; set; }
    
    // Existing functionality
    string SessionId { get; }
    Task LoginAsync(string username, string password);
    Task NewContextAsync();
    
    // Navigation Methods
    Task NavigateAsync(string url);
    Task RefreshAsync();
    Task GoBackAsync();
    Task GoForwardAsync();
    
    // State Properties (async for title)
    string CurrentUrl { get; }
    Task<string> GetTitleAsync();
    bool CanGoBack { get; }
    bool CanGoForward { get; }
    bool IsLoading { get; }
    
    // Content Methods
    Task<string> GetHtmlContentAsync();
    Task<byte[]> TakeScreenshotAsync();
    
    // User Interaction
    Task ClickAsync(double x, double y);
    Task HoverAsync(double x, double y);
    Task TypeAsync(string text);
    Task KeyPressAsync(string key);
    
    // Element-based interactions for BrowserTool
    Task ClickElementAsync(string selector, int timeoutMs = 30000);
    Task FillElementAsync(string selector, string value, int timeoutMs = 30000);
    Task<string?> GetElementTextAsync(string selector, int timeoutMs = 30000);
    Task<string?> GetElementValueAsync(string selector, int timeoutMs = 30000);
    Task WaitForElementAsync(string selector, int timeoutMs = 30000);
    Task ScrollToElementAsync(string selector, int timeoutMs = 30000);
    Task SelectOptionAsync(string selector, string value, string method = "value", int timeoutMs = 30000);
    Task<object?> ExecuteScriptAsync(string script);
    Task ScrollAsync(int deltaX, int deltaY);
    
    Task WaitForLoadAsync();
    Task WaitForStablePageAsync();
    
    // Typed Events
    event NavigationStartedEventHandler NavigationStarted;
    event NavigationCompletedEventHandler NavigationCompleted;
    event NavigationFailedEventHandler NavigationFailed;
    event UrlChangedEventHandler UrlChanged;
    event TitleChangedEventHandler TitleChanged;
    event LoadingStateChangedEventHandler LoadingStateChanged;
    event PageErrorEventHandler PageError;
    event DisconnectedEventHandler Disconnected;
}