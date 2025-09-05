using Microsoft.Playwright;
using RSChatApp.Mcp.Browser.Configuration;
using RSChatApp.Mcp.Browser.Interfaces;

namespace RSChatApp.Mcp.Browser.Infrastructure;

public class PlayWrightBrowserInstance : IBrowserInstance
{
    public IBrowser Browser => _browser;
    public IBrowserContext BrowserContext
    {
        get => _context;
        set => _context = value;
    }

    public IPage Page
    {
        get => _page;
        set => _page = value;
    }

    public string SessionId
    {
        get => _config.SessionId;
    }

    public event EventHandler<IBrowserInstance> Disconnected;
    
    private readonly IBrowser _browser;
    private readonly BrowserInstanceConfiguration _config;
    private IBrowserContext _context;
    private IPage _page;


    public PlayWrightBrowserInstance(BrowserInstanceConfiguration config, IBrowser browser, IBrowserContext context, IPage page)
    {
        _config = config;   
        _browser = browser;
        _context = context;
        _page = page;
        _browser.Disconnected += OnDisconnected;
    }
    private void OnDisconnected(object? sender, IBrowser browser)
    {
        Console.WriteLine("Browser disconnected");
        Disconnected?.Invoke(this, this);
    }
    public async Task LoginAsync(string username, string password)
    {
        throw new NotImplementedException();
    }

    public async Task NewContextAsync()
    {
        throw new NotImplementedException();
    }

    public async Task LogoutAsync()
    {
        throw new NotImplementedException();
    }
    
    public async ValueTask DisposeAsync()
    {
        _browser.Disconnected -= OnDisconnected;
        await _context.DisposeAsync();
        await _browser.DisposeAsync();
    }
}