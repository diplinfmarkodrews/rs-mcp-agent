using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using RSChatApp.Infrastructure.UserInteraction;
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

    [KernelFunction, McpServerTool,  Description("Waits for User confirmation and executes a terminal command on Reportserver")]
    public async Task<string> ExecuteCommandAsync([Description("")] string command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Requesting user confirmation for terminal command: {Command}", command);
        // we need to capture the terminal id before execution as it might be unavailable     
        Guid executingTerminalId = _terminalManager.ActiveTerminalId;
        var userConfirmationResult = await _userConfirmation.RequestUserInteractionAsync(
            new TerminalConfirmRequest("Terminal", command, "bash"));
        if (userConfirmationResult.Result == UserConfirmationResultEnum.Confirmed)
        {
            var result = await _terminalManager.ExecuteAsync(_terminalManager.ActiveTerminalId, command, cancellationToken);
            return JsonSerializer.Serialize(new
            {
                TerminalId = executingTerminalId,
                CommandResult = result
            });
        }
        return $"User has {userConfirmationResult.Result.ToString()} the execution of the terminal command.";
    }
}