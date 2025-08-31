using Microsoft.Extensions.DependencyInjection;
using RSChatApp.Mcp.Browser.Configuration;
using RSChatApp.Mcp.Browser.Core;
using RSChatApp.Mcp.Browser.Infrastructure;
using RSChatApp.Mcp.Browser.Tools;

namespace RSChatApp.Mcp.Browser.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddBrowserTool(this IServiceCollection services, string reportServerUrl)
    {
        
        services.AddHttpContextAccessor();
        services.Configure<BrowserInstanceConfiguration>(config =>
        {
            config.BaseUrl = reportServerUrl;
        });
        services.AddMemoryCache();
        services.AddSingleton<IBrowserInstanceStore, InMemoryBrowserInstanceStore>();
        services.AddSingleton<IBrowserInstanceFactory, PlayWrightBrowserInstanceFactory>();        
        services.AddScoped<IBrowserInstanceProvider, BrowserInstanceProvider>();
        services.AddScoped<BrowserTool>();
        return services;
    }
}