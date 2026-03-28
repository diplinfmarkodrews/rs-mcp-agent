using System.ClientModel;
using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using OpenAI;
using RSChatApp.Application.Core.Chat;
using RSChatApp.Infrastructure.Prompt;
using RSChatApp.Infrastructure.UserInteraction;
using RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient;
using RSChatApp.Shared.Infrastructure.Mcp.SemanticSearch.Mcp;
using RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Mcp;
using RSChatApp.Web.Configuration;
using RSChatApp.Web.HostedServices.McpTool;
using RSChatApp.Web.Mcp.Tools;
using RSChatApp.Web.Models.Auth;
using RSChatApp.Web.Services.Authentication;
using RSChatApp.Web.Services.Chat.Tools;
using RsMcpServer.Identity.Models.Requests;
using Serilog;

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
    
    internal static IServiceCollection AddOpenAIChatClient(this IServiceCollection services, 
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
        services.AddScoped<IAiChatClient, AiChatClientFacade>();
        services.AddKeyedScoped<IChatClient>(serviceKey, (Func<IServiceProvider, object?, IChatClient>)Factory);
        
        return services;
    }

    
    internal static IServiceCollection AddToolCollectionService(this IServiceCollection services)
    {
        // services.AddSingleton<ToolCollectionService>();
        // services.AddHostedService<McpToolCollectionRegistrationHostedService>();
        services.AddScoped<ToolCollectionService>(provider =>
        {
            var grouped = new Dictionary<string, List<AITool>>
            {
                ["Knowledge Base"] =
                [
                    AIFunctionFactory.Create(provider.GetRequiredService<SemanticSearchTool>().SearchAsync, "Search", "Search for information using a phrase or keyword"),
                    AIFunctionFactory.Create(provider.GetRequiredService<DocumentLookupTool>().GetDocumentPage, "GetDocumentPage", "Lookup a page of a given document, optionally with all images."),
                ],
                ["File Store"] =
                [
                    AIFunctionFactory.Create(provider.GetRequiredService<ScriptStoreTool>().GetAllScriptPaths, "GetAllScriptsPath", "Retrieve a list of all scripts path"),
                    AIFunctionFactory.Create(provider.GetRequiredService<ScriptStoreTool>().GetScriptText, "GetTextFromScriptsPath", "Get a text file of a given path. can be scripts or other text files"),
                    AIFunctionFactory.Create(provider.GetRequiredService<ScriptStoreTool>().GetAllSkillsPaths, "GetAllSkillsPath", "Retrieve a list of all skills path"),
                    AIFunctionFactory.Create(provider.GetRequiredService<ScriptStoreTool>().GetSkillsText, "GetTextFromSkillsPath", "Get a text file of a given skills path. can be scripts or other text files"),
                ],
                ["TerminalTool"] =
                [
                    AIFunctionFactory.Create(provider.GetRequiredService<UserConfirmedTerminalTool>().ExecuteCommandAsync, "MultiTerminalTool", "Executes commands in the terminal with user confirmation."),
                ]
            };

            var kernel = provider.GetRequiredService<Kernel>();
            foreach (var plugin in kernel.Plugins)
            {
                grouped[plugin.Name] = plugin.Select(f => (AITool)f.AsAIFunction(kernel)).ToList();
            }

            provider.GetRequiredService<ILogger<Kernel>>()
                .LogInformation("Total tools available: {count}: \n{names}",
                    grouped.Values.Sum(t => t.Count),
                    string.Join(",\n", grouped.Values.SelectMany(t => t).Select(t => t.Name)));

            return new ToolCollectionService(grouped);
        });
        return services;
    }
    
    internal static WebApplicationBuilder AddLoggerConfigs(this WebApplicationBuilder builder)
    {
        // Add Serilog as an additional logging provider alongside OpenTelemetry
        // This allows both Serilog (for console/file) and OpenTelemetry (for Aspire) to work together
        builder.Logging.AddSerilog(new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.File("Logs/rschatapp-.log", rollingInterval: RollingInterval.Day)
            .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
            .WriteTo.Console()
            .CreateLogger(), dispose: true);

        return builder;
    }

    internal static WebApplicationBuilder AddOpenAIConfigs(this WebApplicationBuilder builder)
    {

        return builder;
    }
}
