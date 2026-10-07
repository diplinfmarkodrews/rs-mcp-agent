using RSChatApp.Shared.Infrastructure.Mcp.MetaData;

namespace RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient.Tools;

public record ToolResult(
    string CallId,
    bool IsSuccess,
    ResultContentType ContentType,
    object? Data,
    string? ErrorMessage,
    DateTime CompletedAt
);

