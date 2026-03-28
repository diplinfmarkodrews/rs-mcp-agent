using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using ReportServer.Abstraction;
using ReportServer.Abstraction.Contracts.Terminal;
using RSChatApp.Common;

namespace RSChatApp.Shared.Infrastructure.Mcp.ReportServer.Mcp;

/// <summary>
/// MCP Server implementation for terminal commands using Microsoft.Extensions.AI MCP SDK
/// </summary>
[McpServerToolType]
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
    [McpServerTool, 
     Description("Executes a terminal command on the report server. ")]
    public async Task<TerminalCommandResult> ExecuteCommandAsync(
        [Description("command to be executed in ReportServer terminal")]string command,
        [Description("session id to identify terminal session. Leave empty to start a new session!")]string sessionId = "",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) == false 
            && Guid.TryParse(sessionId, out _) == false)
            sessionId = null;
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            _logger.LogDebug("[TerminalTool]No sessionId provided. Initializing new terminal session.");
            var sessionInfo =  await _reportServer.InitSessionAsync();
            if (sessionInfo.IsSuccess && sessionInfo.Data != null
                && !string.IsNullOrEmpty(sessionInfo.Data.SessionId))
            {
                sessionId = sessionInfo.Data.SessionId;
            }
            else
            {
                return new TerminalCommandResult(
                    sessionId ?? string.Empty,
                    command,
                    Result<CommandResult>.Fail(sessionInfo.Error)
                );
            }
        }
        
        _logger.LogDebug("[TerminalTool]{SessionId}:Executing terminal command: {Command}", sessionId, command);
        
        // Execute the command with the session ID

        var cmdResult = await _reportServer.ExecuteAsync(sessionId, command, cancellationToken);
        return new TerminalCommandResult(
            sessionId,
            command,
            cmdResult);
        
    }
    
    // Command is included in ExecuteCommand due simplicity
    // /// <summary>
    // /// Executes a terminal command on the report server
    // /// </summary>
    // [KernelFunction, McpServerTool, Description("Starts a terminal session on the report server. " +
    //                                             "Returns the session information including session ID. Use the sessionId for subsequent commands.")]
    // public async Task<string> InitTerminalSessionAsync(
    //     CancellationToken cancellationToken = default)
    // {
    //     _logger.LogInformation("Starting terminal session");
    //     var sessionInfo = await _reportServer.InitSessionAsync();
    //     var serializedSessionInfo = JsonSerializer.Serialize(sessionInfo);
    //     _logger.LogDebug("Started terminal session: {SessionInfo}", serializedSessionInfo);
    //     return serializedSessionInfo;
    // }
}
public record TerminalCommandResult(string SessionId, string Command, Result<CommandResult> CmdResult);
