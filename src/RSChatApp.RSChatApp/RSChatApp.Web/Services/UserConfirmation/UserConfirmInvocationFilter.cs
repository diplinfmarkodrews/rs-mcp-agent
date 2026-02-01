using Microsoft.SemanticKernel;
using RSChatApp.Infrastructure.UserInteraction;
using System.Diagnostics;

namespace RSChatApp.Web.Services.UserConfirmation;

public sealed class UserConfirmInvocationFilter : IFunctionInvocationFilter
{
    private readonly IWaitForUserInteraction<TerminalConfirmRequest, UserConfirmationResult> _ui;
    private readonly ILogger<UserConfirmInvocationFilter> _logger;

    public UserConfirmInvocationFilter(
        ILogger<UserConfirmInvocationFilter> logger,
        IWaitForUserInteraction<TerminalConfirmRequest, UserConfirmationResult> ui)
    {
        _logger = logger;
        _ui = ui;
    } 

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext ctx, Func<FunctionInvocationContext, Task> next)
    {
        var pluginName = ctx.Function.Metadata.PluginName ?? string.Empty;
        var functionName = ctx.Function.Metadata.Name ?? ctx.Function.Name ?? string.Empty;

        _logger.LogInformation(
            "SK invoking function {Plugin}.{Function}",
            pluginName,
            functionName);
        try
        {
            // if (TryCreateTerminalConfirmationRequest(ctx, out var request))
            // {
            //     _logger.LogDebug($"UserConfirmation: {request}");
            //     var decision = await _ui.RequestUserInteractionAsync(request);
            //     if (decision.Result != UserConfirmationResultEnum.Confirmed)
            //     {
            //         _logger.LogDebug($"UserConfirmation: {decision} - not confirmed");
            //         ctx.Result = new FunctionResult(ctx.Function, $"User {decision.Result} execution.");
            //         return;
            //     }
            //     _logger.LogDebug($"UserConfirmation: {decision} - confirmed");
            // }
            
            // await next(ctx);
            if (TryCreateTerminalConfirmationRequest(ctx, out var request))
            {
                _logger.LogInformation(
                    "User confirmation requested for {ToolName}",
                    request.ToolName);

                UserConfirmationResult decision;
                try
                {
                    // Tie the wait to the invocation lifetime so we don't silently hang.
                    decision = await _ui.RequestUserInteractionAsync(request)
                        .WaitAsync(ctx.CancellationToken);
                }
                catch (OperationCanceledException oce)
                {
                    _logger.LogWarning(
                        oce,
                        "User confirmation was cancelled before executing {Plugin}.{Function}",
                        pluginName,
                        functionName);

                    ctx.Result = new FunctionResult(ctx.Function, "Tool execution cancelled before confirmation.");
                    return;
                }

                if (decision.Result != UserConfirmationResultEnum.Confirmed)
                {
                    _logger.LogInformation(
                        "User confirmation result for {ToolName}: {Decision}",
                        request.ToolName,
                        decision.Result);

                    ctx.Result = new FunctionResult(ctx.Function, $"User {decision.Result} execution.");
                    return;
                }

                // If the UI allowed editing, prefer the confirmed command (best-effort).
                if (!string.IsNullOrWhiteSpace(decision.Command) && ctx.Arguments is not null)
                {
                    // Common argument names.
                    if (ctx.Arguments.TryGetValue("command", out _)) ctx.Arguments["command"] = decision.Command;
                    if (ctx.Arguments.TryGetValue("cmd", out _)) ctx.Arguments["cmd"] = decision.Command;
                    if (ctx.Arguments.TryGetValue("script", out _)) ctx.Arguments["script"] = decision.Command;
                }

                _logger.LogInformation(
                    "User confirmed execution of {ToolName}",
                    request.ToolName);
            }

            _logger.LogDebug(
                "About to execute SK function body for {Plugin}.{Function} (CancellationRequested={CancellationRequested})",
                pluginName,
                functionName,
                ctx.CancellationToken.IsCancellationRequested);

            var sw = Stopwatch.StartNew();
            // Add a safety timeout while diagnosing "hangs". If this triggers, we'll have a concrete stack/log to chase.
            await next(ctx).WaitAsync(TimeSpan.FromMinutes(2), ctx.CancellationToken);
            sw.Stop();

            _logger.LogDebug(
                "Completed SK function body for {Plugin}.{Function} in {ElapsedMs}ms",
                pluginName,
                functionName,
                sw.ElapsedMilliseconds);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SK invocation failed for {Plugin}.{Function}", pluginName, functionName);
            throw;
        }
    }

    
    private static bool TryCreateTerminalConfirmationRequest(FunctionInvocationContext ctx, out TerminalConfirmRequest request)
    {
        request = default!;

        // Default to allow-list style: only confirm clearly terminal command executions.
        var plugin = ctx.Function.Metadata.PluginName ?? string.Empty;
        var function = ctx.Function.Metadata.Name ?? ctx.Function.Name ?? string.Empty;

        var normalized = NormalizeToolName($"{plugin}.{function}");
        
        // Match RsMcpServer_execute_command or similar terminal execution functions
        bool isTerminalCommand = normalized.Contains("executecommand", StringComparison.Ordinal) 
                                 || (normalized.Contains("rsmcpserver", StringComparison.Ordinal) && normalized.Contains("execute", StringComparison.Ordinal));
        
        // Optionally match browser script execution
        bool isBrowserScript = normalized.Contains("browsertool", StringComparison.Ordinal) 
                               && normalized.Contains("executejavascript", StringComparison.Ordinal);
        
        if (!isTerminalCommand && !isBrowserScript)
        {
            return false;
        }

        // Extract command argument (common spellings).
        var command = TryGetArgAsString(ctx.Arguments, "command")
                      ?? TryGetArgAsString(ctx.Arguments, "cmd")
                      ?? TryGetArgAsString(ctx.Arguments, "script");
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        request = new TerminalConfirmRequest(
            ToolName: string.IsNullOrWhiteSpace(plugin) ? function : $"{plugin}.{function}",
            Command: command,
            Language: isBrowserScript ? "javascript" : "bash");

        return true;
    }

    private static string? TryGetArgAsString(KernelArguments? args, string key)
    {
        if (args is null)
        {
            return null;
        }

        if (!args.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string s => s,
            _ => value.ToString()
        };
    }

    private static string NormalizeToolName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        // Lowercase alphanumerics only, strip "Async" suffix.
        var canonical = name.Trim();
        if (canonical.EndsWith("Async", StringComparison.Ordinal))
        {
            canonical = canonical[..^5];
        }

        Span<char> buffer = stackalloc char[canonical.Length];
        var idx = 0;
        foreach (var ch in canonical)
        {
            if (char.IsLetterOrDigit(ch))
            {
                buffer[idx++] = char.ToLowerInvariant(ch);
            }
        }

        return new string(buffer[..idx]);
    }
}
public record TerminalConfirmRequest(string ToolName, string Command, string Language = "bash");

public record UserConfirmationResult(UserConfirmationResultEnum Result, string? Command = null);

public enum UserConfirmationResultEnum
{
    Confirmed = 1,
    Skipped = 2,
    Cancelled = 3
}

