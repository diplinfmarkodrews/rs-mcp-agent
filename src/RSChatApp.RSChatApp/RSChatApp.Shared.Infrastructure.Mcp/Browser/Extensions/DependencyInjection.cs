using Microsoft.Extensions.DependencyInjection;
using RSChatApp.Shared.Infrastructure.Mcp.Browser.Configuration;
using RSChatApp.Shared.Infrastructure.Mcp.Browser.Implementations;
using RSChatApp.Shared.Infrastructure.Mcp.Browser.Interfaces;

namespace RSChatApp.Shared.Infrastructure.Mcp.Browser.Extensions;

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