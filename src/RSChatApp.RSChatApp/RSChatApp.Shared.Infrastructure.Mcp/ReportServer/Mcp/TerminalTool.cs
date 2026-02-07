using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using ReportServer.Abstraction;

namespace RSChatApp.Shared.Infrastructure.Mcp.ReportServer.Mcp;

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
    [KernelFunction, McpServerTool, Description("Executes a terminal command on the report server. ")]
    public async Task<object> ExecuteCommandAsync(
        [Description("session id to identify terminal session. Leave empty to start a new session!")]string? sessionId,
        [Description("command to be executed in ReportServer terminal")]string command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) == false 
            && Guid.TryParse(sessionId, out _) == false)
            sessionId = null;
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            _logger.LogDebug("[TerminalTool]No sessionId provided. Initializing new terminal session.");
            var sessionInfo =  await _reportServer.InitSessionAsync();
            if (sessionInfo.IsSuccess && sessionInfo.Data != null)
            {
                sessionId = sessionInfo.Data.SessionId;
            }
            else
            {
                return new
                {
                    SessionId = sessionId ?? string.Empty,
                    Command = command,
                    Error = new StringBuilder("Error fetching SessionId.")
                        .Append(sessionInfo.Error?.Message)
                        .ToString()
                };
            }
        }
        
        _logger.LogDebug("[TerminalTool]{SessionId}:Executing terminal command: {Command}", sessionId, command);
        
        // Execute the command with the session ID

        var cmdResult = await _reportServer.ExecuteAsync(sessionId, command, cancellationToken);
        
        return new
        {
            SessionId = sessionId,
            Command = command,
            CmdResult = cmdResult,
        };
        
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
public record TerminalCommandResult(string SessionId, string Command, string CmdResult);
