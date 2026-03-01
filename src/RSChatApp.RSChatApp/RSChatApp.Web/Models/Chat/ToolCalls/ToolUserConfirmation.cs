namespace RSChatApp.Web.Models.Chat.ToolCalls;

public record ToolUserConfirmation(bool RequireToolCallUserConfirmation, bool RequireToolResultUserConfirmation)
{
    public static ToolUserConfirmation None => new(
        RequireToolCallUserConfirmation: false,
        RequireToolResultUserConfirmation: false
    );
    public static ToolUserConfirmation ToolCallOnly => new(
        RequireToolCallUserConfirmation: true,
        RequireToolResultUserConfirmation: false
    );
    public static ToolUserConfirmation ToolCallAndResult => new(
        RequireToolCallUserConfirmation: true,
        RequireToolResultUserConfirmation: true
    );
    public static ToolUserConfirmation ToolResultOnly => new(
        RequireToolCallUserConfirmation: false,
        RequireToolResultUserConfirmation: true
    );
}