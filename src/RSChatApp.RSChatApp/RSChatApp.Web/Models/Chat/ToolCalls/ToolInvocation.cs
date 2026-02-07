namespace RSChatApp.Web.Models.Chat.ToolCalls;

public record ToolInvocation(
    string CallId,
    ToolType Type,
    string RawName,
    string DisplayName,
    IReadOnlyDictionary<string, object?> Parameters,
    ToolMetadata Metadata,
    ToolPermissions Permissions,
    ToolUiHints UiHints
);
