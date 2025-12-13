using RSChatApp.Web.Services.Authentication;

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
        services.AddScoped<IAuthenticationInfoService, AuthenticationInfoService>();
        services.AddScoped<ILoginModalService, LoginModalService>();
        
        return services;
    }
    
}
