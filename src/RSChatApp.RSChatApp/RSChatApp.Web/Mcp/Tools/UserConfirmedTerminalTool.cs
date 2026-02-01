using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using RSChatApp.Infrastructure.UserInteraction;
using RSChatApp.Web.Models.Terminal;
using RSChatApp.Web.Services.Terminal;
using RSChatApp.Web.Services.UserConfirmation;

namespace RSChatApp.Web.Mcp.Tools;

public class UserConfirmedTerminalTool
{
    private readonly ITerminalManager _terminalManager;
    private readonly IWaitForUserInteraction<TerminalConfirmRequest, UserConfirmationResult> _userConfirmation;
    private readonly ILogger<UserConfirmedTerminalTool> _logger;

    public UserConfirmedTerminalTool(ILogger<UserConfirmedTerminalTool> logger, 
        ITerminalManager terminalManager, 
        IWaitForUserInteraction<TerminalConfirmRequest, UserConfirmationResult> userConfirmation)
    {
        _logger = logger;
        _terminalManager = terminalManager;
        _userConfirmation = userConfirmation;
    }

    [KernelFunction, McpServerTool,  Description("Executes a terminal command on Reportserver.  User confirmation is required before execution.")]
    public async Task<string> ExecuteCommandAsync([Description("reportserver terminal command")] string command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Requesting user confirmation for terminal command: {Command}", command);
       
        var userConfirmationResult = await _userConfirmation.RequestUserInteractionAsync(
            new TerminalConfirmRequest("Terminal_executecommand", command, "bash"));
        
        _logger.LogDebug("UserConfirmation result: {Result}", userConfirmationResult.Result);
        if (userConfirmationResult.Result == UserConfirmationResultEnum.Confirmed)
        {
            // we need to capture the terminal id before execution as it might be unavailable     
            var terminalManagerAccess = new TerminalManagerAccess(_terminalManager);
            Guid executingTerminalId = await terminalManagerAccess.GetActiveTerminalIdAsync(TerminalType.ReportServer, cancellationToken);
            _logger.LogDebug("User confirmed execution of terminal command: {Command}", command);
            var result = await _terminalManager.ExecuteAsync(executingTerminalId, command, cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                TerminalId = executingTerminalId,
                SessionId = _terminalManager.ActiveTerminal?.RsSessionId,
                Command = command,
                CommandResult = result
            });
        }
        return $"User has {userConfirmationResult.Result} the execution.";
    }
}