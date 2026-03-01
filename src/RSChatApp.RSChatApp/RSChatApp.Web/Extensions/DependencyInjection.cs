using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;
using RSChatApp.Infrastructure.Prompt;
using RSChatApp.Infrastructure.UserInteraction;
using RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient;
using RSChatApp.Web.Configuration;
using RSChatApp.Web.Models.Auth;
using RSChatApp.Web.Services.Authentication;
using RsMcpServer.Identity.Models.Requests;

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
        services.AddScoped<IWaitForUserInteraction<LoginRequest, LoginResult>, 
            WaitForUserInteraction<LoginRequest, LoginResult>>();
        
        return services;
    }

    public static IServiceCollection AddOpenAIChatClient(this IServiceCollection services, 
        OpenAiSettings openAISettings, 
        string serviceKey,
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
        services.AddScoped<IChatClientFactory, ChatClientFactory>();
        services.AddKeyedScoped<IChatClient>(serviceKey, (Func<IServiceProvider, object?, IChatClient>)Factory);
        
        return services;
    }
  
}
