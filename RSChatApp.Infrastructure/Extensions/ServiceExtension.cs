using Microsoft.Extensions.DependencyInjection;
using RSChatApp.Infrastructure.Identity.Clients;
using RSChatApp.Infrastructure.Identity.Services;

namespace RSChatApp.Infrastructure.Extensions;

public static class ServiceExtension
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Add infrastructure services here
        // Identity
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthenticationClient, LegacyAuthenticationClient>();
        services.AddScoped<ILegacyAuthenticationService, LegacyAuthenticationService>();
        return services;
    
    }
}