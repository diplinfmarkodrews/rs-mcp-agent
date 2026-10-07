namespace RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient.Tools;

public record ToolMetadata(
    string? SessionId,
    DateTime Timestamp,
    string? TargetInfo
);

