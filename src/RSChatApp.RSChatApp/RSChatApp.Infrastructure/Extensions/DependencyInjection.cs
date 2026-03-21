using Microsoft.Extensions.DependencyInjection;
using RSChatApp.Infrastructure.Identity.Clients;
using RSChatApp.Infrastructure.Identity.Services;
using RSChatApp.Infrastructure.Prompt;
using RSChatApp.Infrastructure.ReportServer.Clients;

namespace RSChatApp.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Add infrastructure services here
        // Identity
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthenticationClient, LegacyAuthenticationClient>();
        services.AddScoped<IRsTerminalClient, RsTerminalClient>();
        services.AddScoped<ILegacyAuthenticationService, LegacyAuthenticationService>();
        
        return services;
    
    }
    
    public static IServiceCollection AddPromptServices(this IServiceCollection services)
    {
        services.AddSingleton<IPromptFileStore, PromptFileStore>();
        services.AddScoped<IPromptService, PromptService>();
        services.AddHostedService<PromptStartupValidatorHostedService>();
        return services;
    }
}