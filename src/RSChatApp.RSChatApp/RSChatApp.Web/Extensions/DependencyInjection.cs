using System.ClientModel;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.FileProviders;
using Microsoft.SemanticKernel;
using OpenAI;
using RSChatApp.Infrastructure.Prompt;
using RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Configuration;
using RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Services;
using RSChatApp.Web.Configuration;
using RSChatApp.Web.Services.Authentication;
using RSChatApp.Web.Services.Prompt;

namespace RSChatApp.Web.Extensions;

/// <summary>
/// Extension methods for configuring web app services.
/// </summary>
public static class DependencyInjection
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

    public static IServiceCollection AddOpenAIChatClient(this IServiceCollection services, 
        OpenAISettings openAISettings, 
        string? serviceId = null,
        string? openTelemetrySourceName = null,
        Action<OpenTelemetryChatClient>? openTelemetryConfig = null)
    {
        IChatClient Factory(IServiceProvider serviceProvider, object? _)
        {
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var builder = new OpenAIClient(
                    credential: new ApiKeyCredential(openAISettings.ApiKey),
                    options: new OpenAIClientOptions
                    {
                        Endpoint = new Uri(openAISettings.Url),
                        
                    })
                .GetChatClient(openAISettings.Model)
                .AsIChatClient()
                .AsBuilder()
                .UseFunctionInvocation(loggerFactory)
                .UseOpenTelemetry(loggerFactory, openTelemetrySourceName, openTelemetryConfig);

            if (loggerFactory is not null)
            {
                builder.UseLogging(loggerFactory);
            }
            return builder.Build();
        }

        if (serviceId is null)
        {
            services.AddScoped<IChatClient>(sp => Factory(sp, null));
        }
        else
        {
            services.AddKeyedScoped<IChatClient>(serviceId, (Func<IServiceProvider, object?, IChatClient>)Factory);
        }
        
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
