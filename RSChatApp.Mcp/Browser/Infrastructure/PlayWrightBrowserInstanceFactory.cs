using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using RSChatApp.Mcp.Browser.Configuration;
using RSChatApp.Mcp.Browser.Interfaces;

namespace RSChatApp.Mcp.Browser.Infrastructure;


public class PlayWrightBrowserInstanceFactory :  IBrowserInstanceFactory //BackgroundWorker,
{
    private readonly ILogger<PlayWrightBrowserInstanceFactory> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<BrowserInstanceConfiguration> _browserInstanceConfiguration;

    public PlayWrightBrowserInstanceFactory(ILogger<PlayWrightBrowserInstanceFactory> logger, 
        IOptions<BrowserInstanceConfiguration> browserInstanceConfiguration,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _browserInstanceConfiguration = browserInstanceConfiguration;
    }
    public async Task<IBrowserInstance> CreateInstanceAsync(BrowserInstanceConfiguration config = null)
    {
        if (config == null && _httpContextAccessor.HttpContext == null)
        {
            throw new ArgumentNullException("Both config and HttpContext are null");
        }
        config ??= CreateBrowserConfig(_httpContextAccessor.HttpContext);
        _logger.LogDebug("Creating new instance {config}", config);

        var playwright = await Playwright.CreateAsync();
        IBrowser browser = config.BrowserType.ToLower() switch
        {
            "chromium" => await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                // ChromiumSandbox = true,
                Headless = config.Headless,
                Timeout = config.Timeout
            }),
            "firefox" => await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = config.Headless,
                Timeout = config.Timeout
            }),
            "webkit" => await playwright.Webkit.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = config.Headless,
                Timeout = config.Timeout
            }),
            _ => throw new ArgumentException("Unsupported browser type")
        };
        // ReportProgress(70, "Browser launched");
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = config.UserAgent,
            Locale = config.Language,
            ViewportSize = new ViewportSize { Width = config.Width_Viewport, Height = config.Height_Viewport }
        });
        // ReportProgress(90, "Browser context created");
        var page = await context.NewPageAsync();
        try
        {
            await page.GotoAsync(config.BaseUrl);
        } 
        catch (PlaywrightException ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        
        // ReportProgress(100, "Page loaded");
        return new PlayWrightBrowserInstance(config, browser, context, page);
    }
    private BrowserInstanceConfiguration CreateBrowserConfig(HttpContext context)
    {
        var config = _browserInstanceConfiguration.Value.Clone();
        // set browser and user agent from request headers if available
        if (context.Request.Headers.TryGetValue("User-Agent", out var userAgent))
        {
            config.UserAgent = userAgent.ToString();
            config.SessionId = context.Session.Id;
            // config.BrowserType = context.
        }
        return config;
    }
  
}