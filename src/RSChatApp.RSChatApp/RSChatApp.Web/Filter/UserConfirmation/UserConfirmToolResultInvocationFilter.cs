using Microsoft.SemanticKernel;
using RSChatApp.Infrastructure.UserInteraction;
using RSChatApp.Web.Models.Chat.ToolCalls;
using RSChatApp.Web.Models.Chat.UserConfirmation;
using RSChatApp.Web.Services.Chat;

namespace RSChatApp.Web.Filter.UserConfirmation;

public class UserConfirmToolResultInvocationFilter : IFunctionInvocationFilter
{
    private readonly IWaitForUserInteraction<UserConfirmToolResultRequest, UserConfirmationToolResult> _ui;
    private readonly ILogger<UserConfirmToolCallInvocationFilter> _logger;
    private readonly ToolResultFactory _toolResultFactory;
    private readonly ToolInvocationFactory _toolInvocationFactory;

    public UserConfirmToolResultInvocationFilter(
        ILogger<UserConfirmToolCallInvocationFilter> logger,
        ToolInvocationFactory toolInvocationFactory,
        ToolResultFactory toolResultFactory,
        IWaitForUserInteraction<UserConfirmToolResultRequest, UserConfirmationToolResult> ui)
    {
        _logger = logger;
        _toolInvocationFactory = toolInvocationFactory;
        _toolResultFactory = toolResultFactory;
        _ui = ui;
    }
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        await next(context);
        
        if (TryCreateToolResultConfirmationRequest(context, out var result))
        {
            var userConfirmationResult = await _ui.RequestUserInteractionAsync(result)
                .WaitAsync(context.CancellationToken);
            
            if (userConfirmationResult.UserConfirmationResult == UserConfirmationResultEnum.Confirmed)
            {
                _logger.LogInformation(
                    "User confirmed the result of {ToolName}",
                    result.ToolName);
                
                if (userConfirmationResult.ToolResult != null)
                    context.Result = userConfirmationResult.ToFunctionResult(context.Result, context) 
                                     ?? new FunctionResult(context.Function, "Tool result was confirmed but could not be processed.");
            }
            else
            {
                _logger.LogInformation(
                    "User did not confirm the result of {ToolName}. Result will be cleared.",
                    result.ToolName);
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
    private bool IsUserConfirmationRequired(string pluginName, string functionName, FunctionInvocationContext ctx)
    {
        // TODO: This is currently hardcoded to only trigger for execute_command, but ideally this would be driven by SKFunction metadata or some other more flexible mechanism.
        return functionName == "execute_command";
    }
}




