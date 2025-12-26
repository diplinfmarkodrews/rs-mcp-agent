using Microsoft.JSInterop;
using ReportServer.Abstraction.Contracts;
using ReportServer.Abstraction.Contracts.Terminal;

namespace RSChatApp.Web.Services.Terminal.Drivers;

/// <summary>
/// Terminal driver for JavaScript execution in the browser
/// Uses JSInterop to execute JavaScript code
/// </summary>
public class JsTerminalDriver : ITerminalDriver
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<JsTerminalDriver> _logger;

    public JsTerminalDriver(IJSRuntime jsRuntime, ILogger<JsTerminalDriver> logger)
    {
        _jsRuntime = jsRuntime;
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
            var result = await _jsRuntime.InvokeAsync<string>(
                "eval", 
                cancellationToken, 
                WrapJavaScriptCommand(command));
            
            var commandResult = new CommandResult
            {
                Result = result ?? "(undefined)",
                NewPrompt = "js>",
                Data = string.Empty,
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

    public Task<bool> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    private static string WrapJavaScriptCommand(string command)
    {
        return $@"
(function() {{
    try {{
        const result = {command};
        if (result === undefined) return '(undefined)';
        if (result === null) return '(null)';
        if (typeof result === 'object') return JSON.stringify(result, null, 2);
        return String(result);
    }} catch (error) {{
        throw new Error(error.message);
    }}
}})()";
    }
}
