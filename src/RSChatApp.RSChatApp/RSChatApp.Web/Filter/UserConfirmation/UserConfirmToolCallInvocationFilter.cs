using System.Text.Json;
using Microsoft.SemanticKernel;
using RSChatApp.Infrastructure.UserInteraction;
using RSChatApp.Web.Models.Chat.UserConfirmation;
using RSChatApp.Web.Services.Chat.Tools;

namespace RSChatApp.Web.Filter.UserConfirmation;

public sealed class UserConfirmToolCallInvocationFilter : IFunctionInvocationFilter
{
    private readonly IWaitForUserInteraction<UserConfirmToolCallRequest, UserConfirmationToolCall> _ui;
    private readonly ILogger<UserConfirmToolCallInvocationFilter> _logger;
    private readonly ToolRegistry _toolRegistry;

    public UserConfirmToolCallInvocationFilter(
        ILogger<UserConfirmToolCallInvocationFilter> logger,
        ToolRegistry toolRegistry,
        IWaitForUserInteraction<UserConfirmToolCallRequest, UserConfirmationToolCall> ui)
    {
        _logger = logger;
        _ui = ui;
        _toolRegistry = toolRegistry;
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
            if (TryCreateUserConfirmToolCallRequest(ctx, out var request))
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

    
    private bool TryCreateUserConfirmToolCallRequest(FunctionInvocationContext ctx, out UserConfirmToolCallRequest request)
    {
        request = default!;
        
        var plugin = ctx.Function.Metadata.PluginName ?? string.Empty;
        var function = ctx.Function.Metadata.Name ?? ctx.Function.Name ?? string.Empty;
        var toolDescriptor = _toolRegistry.GetDescriptor($"{plugin}.{function}"); 
        
        var toolUserConfirmationRequire = toolDescriptor.GetUserConfirmation(function);
        
        if (toolUserConfirmationRequire.RequireToolCallUserConfirmation)
        {
            var arguments = ctx.Arguments?.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value?.ToString()) ?? new Dictionary<string, object?>();
            
            request = new UserConfirmToolCallRequest
            {
                ToolName = string.IsNullOrWhiteSpace(plugin) ? function : $"{plugin}.{function}",
                Arguments = arguments
            };
            return true;
        }
        
        return false;
    }
    
}
