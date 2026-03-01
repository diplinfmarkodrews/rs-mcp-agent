using RSChatApp.Shared.Infrastructure.Mcp.MetaData;

namespace RSChatApp.Web.Models.Chat.ToolCalls;

public record ToolResult(
    string CallId,
    bool IsSuccess,
    ResultContentType ContentType,
    object? Data,
    string? ErrorMessage,
    DateTime CompletedAt
);
