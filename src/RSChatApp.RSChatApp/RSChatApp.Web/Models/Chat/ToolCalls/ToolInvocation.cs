using RSChatApp.Shared.Infrastructure.Mcp.MetaData;

namespace RSChatApp.Web.Models.Chat.ToolCalls;

public record ToolInvocation(
    string CallId,
    ToolType Type,
    ResultContentType ResultContentType,
    string RawName,
    string DisplayName,
    IReadOnlyDictionary<string, object?> Parameters,
    ToolMetadata Metadata,
    ToolPermissions Permissions,
    ToolUiHints UiHints
);
