using Microsoft.Extensions.DependencyInjection;
using RsMcpServer.Identity.Services;

namespace RsMcpServer.Identity.Extensions;

/// <summary>
/// Extension methods for registering authentication services
/// </summary>
public static class AuthenticationServiceExtensions
{
    /// <summary>
    /// Add legacy authentication services for direct RsMcpServer access
    /// </summary>
    public static IServiceCollection AddLegacyAuthentication(this IServiceCollection services)
    {
        // Session store
        services.AddSingleton<ISessionStore, InMemorySessionStore>();
        
        // Main authentication service (contains all logic now)
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        
        return services;
    }
}
