using System.ClientModel;
using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using OpenAI;
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
    
    internal static IServiceCollection AddToolCollectionService(this IServiceCollection services)
    {
        // services.AddSingleton<ToolCollectionService>();
        // services.AddHostedService<McpToolCollectionRegistrationHostedService>();
        services.AddScoped<ToolCollectionService>(provider =>
        {
            var allTools = new List<AITool>
            {
                AIFunctionFactory.Create(provider.GetRequiredService<SemanticSearchTool>().SearchAsync,  "Search", "Search for information using a phrase or keyword"),
                AIFunctionFactory.Create(provider.GetRequiredService<DocumentLookupTool>().GetDocumentPage, "GetDocumentPage", "Lookup a page of a given document, optionally with all images."),
                AIFunctionFactory.Create(provider.GetRequiredService<ScriptStoreTool>().GetAllScriptPaths, "GetAllScriptsPath", "Retrieve a list of all scripts path"),
                AIFunctionFactory.Create(provider.GetRequiredService<ScriptStoreTool>().GetScriptText, "GetTextFromScriptsPath","Get a text file of a given path. can be scripts or other text files"),
                AIFunctionFactory.Create(provider.GetRequiredService<ScriptStoreTool>().GetAllSkillsPaths, "GetAllSkillsPath", "Retrieve a list of all skills path"),
                AIFunctionFactory.Create(provider.GetRequiredService<ScriptStoreTool>().GetSkillsText, "GetTextFromSkillsPath","Get a text file of a given skills path. can be scripts or other text files"),
                // AIFunctionFactory.Create(AuthenticationTool.IsAuthenticatedAsync, "IsAuthenticated", "Checks whether the user is authenticated against the ReportServer and can execute ReportServerMcp tools or not"),
                // AIFunctionFactory.Create(AuthenticationTool.LoginUserRequestedAsync, "RequestLogin", "Requests the user to login when they need to access ReportServer MCP tools but are not authenticated"),
                // AIFunctionFactory.Create(provider.GetRequiredService<UserConfirmedTerminalTool>().ExecuteCommandAsync, "MultiTerminalTool", "Executes commands in the terminal with user confirmation. Valid terminal types are ")
            };
            var kernel = provider.GetRequiredService<Kernel>();
            var kernelPlugins = kernel.Plugins;
            foreach (var plugin in kernelPlugins)
            {
                foreach (var aiFunction in plugin)
                {
                    allTools.Add(aiFunction.AsAIFunction(kernel));
                }
            }
            provider.GetRequiredService<ILogger<Kernel>>()
                .LogInformation("Total tools available: {kernelToolCount}: \n{kernelToolsNames} ", allTools.Count, 
                    string.Join(",\n", allTools.Select(p=> p.Name)));
            
            return new ToolCollectionService(allTools);
        });
        return services;
    }
}
