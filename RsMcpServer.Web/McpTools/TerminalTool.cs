using System.ComponentModel;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using ReportServerPort;
using ReportServerPort.Contracts;
using ReportServerPort.Contracts.Terminal;
using RsMcpServer.Identity.Services;

namespace RsMcpServer.Web.McpTools;

/// <summary>
/// MCP Server implementation for terminal commands using Microsoft.Extensions.AI MCP SDK
/// </summary>
public class TerminalTool
{
    // private readonly ILogger<TerminalTool> _logger;
    private readonly IReportServerClient _reportServer;
    private readonly ISessionBridgeService _sessionBridge;

    public TerminalTool(
        // ILogger<TerminalTool> logger, 
        IReportServerClient reportServer,
        ISessionBridgeService sessionBridge)
    {
        // _logger = logger;
        _reportServer = reportServer;
        _sessionBridge = sessionBridge;
    }

    /// <summary>
    /// Executes a terminal command on the report server
    /// </summary>
    [KernelFunction, McpServerTool, Description("Executes a terminal command on the report server")]
    public async Task<Result<CommandResult>> ExecuteCommandAsync(string command,
        CancellationToken cancellationToken = default)
    {
        // _logger.LogInformation("Executing terminal command: {Command}", command);
        
        // Get the session information from the session bridge service
        var sessionInfo = await _sessionBridge.GetSessionInfoAsync();
        
        if (sessionInfo?.ReportServerSessionId == null)
        {
            // _logger.LogWarning("No active ReportServer session available. Authentication required.");
            return new Result<CommandResult>(new AuthenticationException("Authentication required. Please authenticate with the Report Server first."));
        }
        
        // Execute the command with the session ID
        // TODO: make it long running
        var cmdResult = await _reportServer.ExecuteAsync(sessionInfo.ReportServerSessionId, command);
        return cmdResult;
    }
}

