using Microsoft.Extensions.DependencyInjection;
using RSChatApp.Mcp.Browser.Configuration;
using RSChatApp.Mcp.Browser.Implementations;
using RSChatApp.Mcp.Browser.Interfaces;

namespace RSChatApp.Mcp.Browser.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddBrowserInstance(this IServiceCollection services, string reportServerUrl)
    {
        services.AddHttpContextAccessor();
        services.Configure<BrowserInstanceConfiguration>(config =>
        {
            config.BaseUrl = reportServerUrl;
        });
        // Add LazyCache, adds InMemoryCache as well
        // We use it to block access to browser instance from multiple threads
        services.AddLazyCache();
        services.AddSingleton<IBrowserInstanceStore, InMemoryBrowserInstanceStore>();
        services.AddSingleton<IBrowserInstanceFactory, PlayWrightBrowserInstanceFactory>();        
        services.AddSingleton<IBrowserInstanceProvider, BrowserInstanceProvider>();
        
        return services;
    }
}