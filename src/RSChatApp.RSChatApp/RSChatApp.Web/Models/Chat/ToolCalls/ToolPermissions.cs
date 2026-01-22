namespace RSChatApp.Web.Models.Chat.ToolCalls;

public record ToolPermissions(
    bool CanRerun,
    bool CanEditResult,
    bool CanCopy,
    bool CanExpand
)
{
    public static ToolPermissions Default => new(
        CanRerun: true,
        CanEditResult: false,
        CanCopy: true,
        CanExpand: true
    );

    public static ToolPermissions ReadOnly => new(
        CanRerun: false,
        CanEditResult: false,
        CanCopy: true,
        CanExpand: true
    );
}
