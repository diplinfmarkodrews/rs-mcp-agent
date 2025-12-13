using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using ReportServer.Abstraction;
using RsMcpServer.Identity.Services;

namespace RSChatApp.Mcp.ReportServer.Tools;

/// <summary>
/// MCP Server implementation for terminal commands using Microsoft.Extensions.AI MCP SDK
/// </summary>

public class TerminalTool
{
    private readonly ILogger<TerminalTool> _logger;
    private readonly IReportServerClient _reportServer;
    
    public TerminalTool(
        ILogger<TerminalTool> logger, 
        IReportServerClient reportServer)
    {
        _logger = logger;
        _reportServer = reportServer;
    }

    /// <summary>
    /// Executes a terminal command on the report server
    /// </summary>
    [KernelFunction, McpServerTool, Description("Executes a terminal command on the report server. " +
                                                "Before executing commands, start a terminal session using " +
                                                "StartTerminalSessionAsync to get a sessionId.")]
    public async Task<string> ExecuteCommandAsync(
        [Description("session id to identify terminal session")] string sessionId,
        [Description("command to be executed in ReportServer terminal")]string command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("{SessionId}:Executing terminal command: {Command}", sessionId, command);
        // Execute the command with the session ID
        var cmdResult = await _reportServer.ExecuteAsync(sessionId, command, cancellationToken);
        _logger.LogDebug("terminal returned: {CmdResult}", JsonSerializer.Serialize(cmdResult));
        return JsonSerializer.Serialize(cmdResult);
    }
    /// <summary>
    /// Executes a terminal command on the report server
    /// </summary>
    [KernelFunction, McpServerTool, Description("Starts a terminal session on the report server. " +
                                                "Returns the session information including session ID. Use the sessionId for subsequent commands.")]
    public async Task<string> InitTerminalSessionAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting terminal session");
        var sessionInfo = await _reportServer.InitSessionAsync();
        var serializedSessionInfo = JsonSerializer.Serialize(sessionInfo);
        _logger.LogDebug("Started terminal session: {SessionInfo}", serializedSessionInfo);
        return serializedSessionInfo;
    }
}

