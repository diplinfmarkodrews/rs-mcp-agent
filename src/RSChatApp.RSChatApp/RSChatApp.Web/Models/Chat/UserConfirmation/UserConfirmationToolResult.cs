using Microsoft.SemanticKernel;
using RSChatApp.Web.Models.Chat.ToolCalls;

namespace RSChatApp.Web.Models.Chat.UserConfirmation;

public class UserConfirmationToolResult
{
    public UserConfirmationResultEnum UserConfirmationResult { get; set; }
    public ToolResult? ToolResult { get; set; } 
    public FunctionResult? ToFunctionResult(FunctionResult fct, FunctionInvocationContext ctx)
    {
        if (ToolResult == null) return null;
        return new FunctionResult(ctx.Function, ToolResult.Data, fct.Culture, fct.Metadata);
    }
    public static UserConfirmationToolResult Confirmed(ToolResult toolResult)
        => new UserConfirmationToolResult
        {
            UserConfirmationResult = UserConfirmationResultEnum.Confirmed,
            ToolResult = toolResult
        };
    
    public static UserConfirmationToolResult Cancelled
        => new UserConfirmationToolResult
        {
            UserConfirmationResult = UserConfirmationResultEnum.Cancelled,
        };
    public static UserConfirmationToolResult Skipped
        => new UserConfirmationToolResult
        {
            UserConfirmationResult = UserConfirmationResultEnum.Skipped,
        };
}

public enum UserConfirmationResultEnum
{
    Confirmed = 1,
    Skipped = 2,
    Cancelled = 3,
    Redacted = 4
}