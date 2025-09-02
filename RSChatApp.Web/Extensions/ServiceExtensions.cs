using RSChatApp.Web.Services.Authentication;
using RSChatApp.Web.Services.Browser;

namespace RSChatApp.Web.Extensions;

/// <summary>
/// Extension methods for configuring authentication services
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds the custom authentication service to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCustomAuthenticationService(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticationService, BlazorAuthenticationService>();
        services.AddScoped<ILoginModalService, LoginModalService>();
        return services;
    }
    public static IServiceCollection AddBrowserStreamingService(this IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
        });

        // Register the browser streaming service
        services.AddSingleton<IBrowserStreamingService, BrowserStreamingService>();

        
        // Optional: Add CORS if needed for external access
        // services.AddCors(options =>
        // {
        //     options.AddDefaultPolicy(policy =>
        //     {
        //         policy.WithOrigins("https://localhost:7000") // Your app URL
        //             .AllowAnyHeader()
        //             .AllowAnyMethod()
        //             .AllowCredentials();
        //     });
        // });

        

        return services;
    }
}
