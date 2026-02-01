namespace RSChatApp.Web.Models.Chat.ToolCalls;

public record ToolMetadata(
    string? SessionId,
    DateTime Timestamp,
    string? TargetInfo
);
