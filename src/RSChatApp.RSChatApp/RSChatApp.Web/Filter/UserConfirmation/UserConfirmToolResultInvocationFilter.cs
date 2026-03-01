using Microsoft.SemanticKernel;
using RSChatApp.Infrastructure.UserInteraction;
using RSChatApp.Web.Models.Chat.UserConfirmation;
using RSChatApp.Web.Services.Chat.Tools;

namespace RSChatApp.Web.Filter.UserConfirmation;

public class UserConfirmToolResultInvocationFilter : IFunctionInvocationFilter
{
    private readonly IWaitForUserInteraction<UserConfirmToolResultRequest, UserConfirmationToolResult> _ui;
    private readonly ILogger<UserConfirmToolCallInvocationFilter> _logger;
    private readonly ToolResultFactory _toolResultFactory;
    
    private readonly ToolInvocationFactory _toolInvocationFactory;
    private readonly ToolRegistry _toolRegistry;

    public UserConfirmToolResultInvocationFilter(
        ILogger<UserConfirmToolCallInvocationFilter> logger,
        ToolInvocationFactory toolInvocationFactory,
        ToolResultFactory toolResultFactory,
        ToolRegistry toolRegistry,
        IWaitForUserInteraction<UserConfirmToolResultRequest, UserConfirmationToolResult> ui)
    {
        _logger = logger;
        _toolInvocationFactory = toolInvocationFactory;
        _toolResultFactory = toolResultFactory;
        _toolRegistry = toolRegistry;
        _ui = ui;
    }
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        await next(context);
        // next.Target
        if (TryCreateToolResultConfirmationRequest(context, out var confirmToolResultRequest))
        {
            var userConfirmationResult = await _ui.RequestUserInteractionAsync(confirmToolResultRequest)
                .WaitAsync(context.CancellationToken);
            
            if (userConfirmationResult.UserConfirmationResult == UserConfirmationResultEnum.Confirmed
                || userConfirmationResult.UserConfirmationResult == UserConfirmationResultEnum.Redacted)
            {
                _logger.LogInformation(
                    "User confirmed the result of {ToolName}",
                    confirmToolResultRequest.ToolName);
                
                // Update only if we redacted toolResult
                if (userConfirmationResult.ToolResult != null 
                    && userConfirmationResult.UserConfirmationResult == UserConfirmationResultEnum.Redacted)
                    context.Result = userConfirmationResult.ToFunctionResult(context.Result, context) 
                                     ?? new FunctionResult(context.Function, "Tool result was confirmed but could not be processed.");
            }
            else
            {
                _logger.LogInformation(
                    "User did not confirm the result of {ToolName}. Result will be cleared.",
                    confirmToolResultRequest.ToolName);
                context.Result = new FunctionResult(context.Function, "Tool result was not confirmed by the user.");
            }
        }
    }

    private bool TryCreateToolResultConfirmationRequest(FunctionInvocationContext ctx,
        out UserConfirmToolResultRequest request)
    {
        request = default;
        var pluginName = ctx.Function.Metadata.PluginName ?? string.Empty;
        var functionName = ctx.Function.Metadata.Name ?? ctx.Function.Name ?? string.Empty;
        
        if (IsUserConfirmationRequired(pluginName, functionName, ctx))
        {
            var toolInvocation = _toolInvocationFactory.Create(ctx.Function);
            var toolResult = _toolResultFactory.Create(ctx.Result, toolInvocation);
            request = new UserConfirmToolResultRequest($"{pluginName}.{functionName}", toolInvocation, toolResult);
            return true;
        }

        return false;
    }
    
    private bool IsUserConfirmationRequired(string plugin, string function, FunctionInvocationContext ctx)
    {
        if(ctx.Arguments.TryGetValue("IsLocalModel", out var isLocalModel)
           && isLocalModel is bool localModelFlag && localModelFlag)
        {
            // Local model tool calls don't require user confirmation for the result as they won't distribute sensitive data.
            return false;
        }
        var toolDescriptor = _toolRegistry.GetDescriptor($"{plugin}.{function}"); 
        var toolUserConfirmationRequire = toolDescriptor.GetUserConfirmation(function);
        if (toolUserConfirmationRequire.RequireToolResultUserConfirmation)
        {
            return true;
        }
        
        return false;
        
    }
}




