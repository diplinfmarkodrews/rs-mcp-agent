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
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptions<BrowserInstanceConfiguration> _browserInstanceConfiguration;

    public PlayWrightBrowserInstanceFactory(ILoggerFactory loggerFactory, 
        IOptions<BrowserInstanceConfiguration> browserInstanceConfiguration,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = loggerFactory.CreateLogger<PlayWrightBrowserInstanceFactory>();
        _loggerFactory = loggerFactory;
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
        var browserInstance = new PlayWrightBrowserInstance(
            _loggerFactory.CreateLogger<PlayWrightBrowserInstance>(),
            config,
            browser);
        // ReportProgress(100, "Page loaded");
        await browserInstance.NewContextAsync();
        return browserInstance;
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