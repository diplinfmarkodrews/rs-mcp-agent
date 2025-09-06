namespace RSChatApp.Mcp.Browser.Configuration;

public class BrowserInstanceConfiguration
{
    public string SessionId { get; set; }
    public string BrowserType { get; set; } = "chromium"; // or "firefox", "webkit"
    public bool Headless { get; set; } = false;
    public int Timeout { get; set; } = 30000; // in milliseconds
    public string UserAgent { get; set; } = "RSChatAppBot/1.0";
    public string Language { get; set; } = "de-DE";
    public int Width_Viewport { get; set; } = 1280;
    public int Height_Viewport { get; set; } = 800;
    public string BaseUrl { get; set; }
    
    // Debounce Configuration (in milliseconds)
    public int NavigationDebounceMs { get; set; } = 300;
    public int ContentRefreshDebounceMs { get; set; } = 150;
    public int UserInteractionDebounceMs { get; set; } = 100;
    
    public BrowserInstanceConfiguration Clone()
    {
        return new BrowserInstanceConfiguration(this);
    }
    public BrowserInstanceConfiguration() { }
    
    private BrowserInstanceConfiguration(BrowserInstanceConfiguration config)
    {
        SessionId = config.SessionId;
        BrowserType = config.BrowserType;
        Headless = config.Headless;
        Timeout = config.Timeout;
        UserAgent = config.UserAgent;
        Language = config.Language;
        Width_Viewport = config.Width_Viewport;
        Height_Viewport = config.Height_Viewport;
        BaseUrl = config.BaseUrl;
        NavigationDebounceMs = config.NavigationDebounceMs;
        ContentRefreshDebounceMs = config.ContentRefreshDebounceMs;
        UserInteractionDebounceMs = config.UserInteractionDebounceMs;
    }
}