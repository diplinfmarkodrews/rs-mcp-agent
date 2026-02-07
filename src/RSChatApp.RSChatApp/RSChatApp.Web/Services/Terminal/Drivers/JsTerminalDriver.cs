using System.Text.Json;
using Microsoft.JSInterop;
using ReportServer.Abstraction.Contracts;
using ReportServer.Abstraction.Contracts.Terminal;
using RSChatApp.Common;
using RSChatApp.Shared.Infrastructure.Mcp.Browser.Interfaces;
using RSChatApp.Web.Models.Terminal;

namespace RSChatApp.Web.Services.Terminal.Drivers;

/// <summary>
/// Terminal driver for JavaScript execution in the browser
/// Uses JSInterop to execute JavaScript code
/// </summary>
public class JsTerminalDriver : ITerminalDriver
{
    private readonly IBrowserInstance _browserInstance;
    private readonly ILogger<JsTerminalDriver> _logger;

    public JsTerminalDriver(IBrowserInstanceProvider browserInstanceProvider, ILogger<JsTerminalDriver> logger)
    {
        _browserInstance = browserInstanceProvider.GetBrowserInstance();
        _logger = logger;
    }

    public Task<Result<TerminalSessionInfo>> InitSessionAsync(CancellationToken cancellationToken)
    {
        var sessionInfo = new TerminalSessionInfo
        {
            SessionId = Guid.NewGuid().ToString(),
            Prompt = "js>",
            WorkingDirectory = "browser"
        };
        return Task.FromResult(Result<TerminalSessionInfo>.Success(sessionInfo));
    }

    public async Task<Result<CommandResult>> ExecuteCommandAsync(
        string sessionId, 
        string command, 
        CancellationToken cancellationToken)
    {
        try
        {
        
            var result = await _browserInstance.ExecuteScriptAsync(command);
            if (result is null)
               return Result<CommandResult>.Fail(new Exception("Null result from JS execution"));
            
            var commandResult = new CommandResult
            {
                Result =  JsonSerializer.Serialize(result),
                NewPrompt = "js>",
                Data = result ?? string.Empty,
                Error = string.Empty
            };
            
            return Result<CommandResult>.Success(commandResult);
        }
        catch (JSException jsEx)
        {
            _logger.LogWarning(jsEx, "JavaScript execution error");
            var errorResult = new CommandResult
            {
                Result = string.Empty,
                Error = jsEx.Message,
                NewPrompt = "js>",
                Data = string.Empty
            };
            return new Result<CommandResult>(jsEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing JavaScript command");
            return new Result<CommandResult>(ex);
        }
    }

    public Task<Result> CloseSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<bool> ValidateSessionAsync(TerminalInstance terminal, SessionContext sessionContext, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
}
