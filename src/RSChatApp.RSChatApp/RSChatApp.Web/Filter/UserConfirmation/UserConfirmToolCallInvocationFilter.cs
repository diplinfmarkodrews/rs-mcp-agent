using System.Text.Json;
using Microsoft.SemanticKernel;
using RSChatApp.Infrastructure.UserInteraction;
using RSChatApp.Web.Models.Chat.UserConfirmation;

namespace RSChatApp.Web.Filter.UserConfirmation;

public sealed class UserConfirmToolCallInvocationFilter : IFunctionInvocationFilter
{
    private readonly IWaitForUserInteraction<UserConfirmToolCallRequest, UserConfirmationToolCall> _ui;
    private readonly ILogger<UserConfirmToolCallInvocationFilter> _logger;

    public UserConfirmToolCallInvocationFilter(
        ILogger<UserConfirmToolCallInvocationFilter> logger,
        IWaitForUserInteraction<UserConfirmToolCallRequest, UserConfirmationToolCall> ui)
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
            if (TryCreateTerminalConfirmationRequest(ctx, out var request))
            {
                _logger.LogInformation(
                    "User confirmation requested for {ToolName}",
                    request.ToolName);

                UserConfirmationToolCall decision;
                try
                {
                    // Tie the wait to the invocation lifetime so we don't silently hang.
                    decision = await _ui.RequestUserInteractionAsync(request)
                        .WaitAsync(ctx.CancellationToken);
                }
                catch (OperationCanceledException oce) // Canceled
                {
                    _logger.LogWarning(
                        oce,
                        "User confirmation was cancelled before executing {Plugin}.{Function}",
                        pluginName,
                        functionName);

                    ctx.Result = new FunctionResult(ctx.Function, "Tool execution cancelled before confirmation.");
                    return;
                }

                if (decision.Result != UserConfirmationResultEnum.Confirmed) // Skipped
                {
                    _logger.LogInformation(
                        "User confirmation result for {ToolName}: {Decision}",
                        request.ToolName,
                        decision.Result);

                    ctx.Result = new FunctionResult(ctx.Function, $"User {decision.Result} execution.");
                    return;
                }

                // If the UI allowed editing, prefer the confirmed command (best-effort).
                if (decision.Arguments is not null && ctx.Arguments is not null)
                {
                    // Common argument names.
                    foreach (var arg in decision.Arguments)
                        ctx.Arguments[arg.Key] = arg.Value;   
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
            
            await next(ctx).WaitAsync(ctx.CancellationToken);
        }
        catch (Exception ex)
        {
            // Dont crash! return detailed error result
            _logger.LogError(ex, "SK invocation failed for {Plugin}.{Function}", pluginName, functionName);
            ctx.Result = new FunctionResult(ctx.Function, $"error: SK invocation failed for {pluginName}.{functionName}:  {ex.Message}");
            return;
        }
    }

    
    private static bool TryCreateTerminalConfirmationRequest(FunctionInvocationContext ctx, out UserConfirmToolCallRequest request)
    {
        request = default!;

        // Default to allow-list style: only confirm clearly terminal command executions.
        var plugin = ctx.Function.Metadata.PluginName ?? string.Empty;
        var function = ctx.Function.Metadata.Name ?? ctx.Function.Name ?? string.Empty;

        var normalized = NormalizeToolName($"{plugin}.{function}");
        
        // Match RsMcpServer_execute_command or similar terminal execution functions
        bool isTerminalCommand = normalized.Contains("executecommand", StringComparison.Ordinal) 
                                 || (normalized.Contains("terminaltool", StringComparison.Ordinal) 
                                     && normalized.Contains("execute", StringComparison.Ordinal));
        
        // Optionally match browser script execution
        bool isBrowserScript = normalized.Contains("browsertool", StringComparison.Ordinal) 
                               && normalized.Contains("executejavascript", StringComparison.Ordinal);
        
        if (!isTerminalCommand && !isBrowserScript)
        {
            return false;
        }

        if (isTerminalCommand && TryGetArgAsString(ctx.Arguments, "command", out var command))
        {
            if (string.IsNullOrWhiteSpace(command))
                return false;
            
            request = new UserConfirmToolCallRequest
            {
                ToolName = string.IsNullOrWhiteSpace(plugin) ? function : $"{plugin}.{function}",
                Arguments = new Dictionary<string, object?>
                {
                    { "command", command }
                }
            };
            return true;
        }
        if (isBrowserScript && TryGetArgAsString(ctx.Arguments, "script", out var script))
        {
            if (string.IsNullOrWhiteSpace(script))
                return false;
            
            request = new UserConfirmToolCallRequest
            {
                ToolName = string.IsNullOrWhiteSpace(plugin) ? function : $"{plugin}.{function}",
                Arguments = new Dictionary<string, object?>
                {
                    { "script", script }
                }
            };
            return true;
        }

        return false;
    }

    private static bool TryGetArgAsString(KernelArguments? args, string key, out string? stringValue)
    {
        stringValue = null;
            
        if (args is null)
            return false;
        
        if (!args.TryGetValue(key, out var value) || value is null)
            return false;

        switch (value)
        {
            case string:
                stringValue = (string)value;
                return true;
            case JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.String:
                stringValue = jsonElement.GetString();
                return true;
            default:
                try
                {
                    stringValue = JsonSerializer.Serialize(value, typeof(object));
                    return true;
                }
                catch
                {
                    return false;
                }
        }
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
