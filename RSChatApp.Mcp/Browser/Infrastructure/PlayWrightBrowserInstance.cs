using Microsoft.Playwright;
using RSChatApp.Mcp.Browser.Configuration;
using RSChatApp.Mcp.Browser.Interfaces;

namespace RSChatApp.Mcp.Browser.Infrastructure;

public class PlayWrightBrowserInstance : IBrowserInstance
{
    public IBrowser Browser => _instance;
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

    private readonly IBrowser _instance;
    private readonly BrowserInstanceConfiguration _config;
    private IBrowserContext _context;
    private IPage _page;


    public PlayWrightBrowserInstance(BrowserInstanceConfiguration config, IBrowser browser, IBrowserContext context, IPage page)
    {
        _config = config;   
        _instance = browser;
        _context = context;
        _page = page;
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
        await _context.DisposeAsync();
        await _instance.DisposeAsync();
    }
}