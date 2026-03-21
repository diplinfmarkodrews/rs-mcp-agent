using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using RSChatApp.Shared.Infrastructure.Mcp.SemanticSearch.Mcp;
using RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Mcp;
using RSChatApp.Web.Services.Chat.Tools;

namespace RSChatApp.Web.HostedServices.McpTool;

public class McpToolCollectionRegistrationHostedService: IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _provider;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<Program> _startupLogger;
    

    public McpToolCollectionRegistrationHostedService(
        IConfiguration configuration,
        IServiceProvider provider,
        ILoggerFactory loggerFactory,
        ILogger<Program> startupLogger)
    {
        _configuration = configuration;
        _loggerFactory = loggerFactory;
        _startupLogger = startupLogger;
        _provider = provider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _provider.CreateScope();
        var toolCollectionService = _provider.GetRequiredService<ToolCollectionService>();
        var allTools = new List<AITool>
            {
                AIFunctionFactory.Create(scope.ServiceProvider.GetRequiredService<SemanticSearchTool>().SearchAsync,  "Search", "Search for information using a phrase or keyword"),
                AIFunctionFactory.Create(scope.ServiceProvider.GetRequiredService<DocumentLookupTool>().GetDocumentPage, "GetDocumentPage", "Lookup a page of a given document, optionally with all images."),
                AIFunctionFactory.Create(scope.ServiceProvider.GetRequiredService<ScriptStoreTool>().GetAllScriptPaths, "GetAllScriptsPath", "Retrieve a list of all scripts path"),
                AIFunctionFactory.Create(scope.ServiceProvider.GetRequiredService<ScriptStoreTool>().GetScriptText, "GetTextFromScriptsPath","Get a text file of a given path. can be scripts or other text files"),
                AIFunctionFactory.Create(scope.ServiceProvider.GetRequiredService<ScriptStoreTool>().GetAllSkillsPaths, "GetAllSkillsPath", "Retrieve a list of all skills path"),
                AIFunctionFactory.Create(scope.ServiceProvider.GetRequiredService<ScriptStoreTool>().GetSkillsText, "GetTextFromSkillsPath","Get a text file of a given skills path. can be scripts or other text files"),
                // AIFunctionFactory.Create(AuthenticationTool.IsAuthenticatedAsync, "IsAuthenticated", "Checks whether the user is authenticated against the ReportServer and can execute ReportServerMcp tools or not"),
                // AIFunctionFactory.Create(AuthenticationTool.LoginUserRequestedAsync, "RequestLogin", "Requests the user to login when they need to access ReportServer MCP tools but are not authenticated"),
                // AIFunctionFactory.Create(provider.GetRequiredService<UserConfirmedTerminalTool>().ExecuteCommandAsync, "MultiTerminalTool", "Executes commands in the terminal with user confirmation. Valid terminal types are ")
            };
            var kernel = scope.ServiceProvider.GetRequiredService<Kernel>();
            var kernelPlugins = kernel.Plugins;
            foreach (var plugin in kernelPlugins)
            {
                foreach (var aiFunction in plugin)
                {
                    allTools.Add(aiFunction.AsAIFunction(kernel));
                }
            }

        toolCollectionService.AllTools.AddRange(allTools);
        _startupLogger.LogInformation(
            "Register tools: {ToolCalls}",
            new StringBuilder().AppendJoin(",\n", allTools.Select(t => t.Name)));

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
      
    }
}
