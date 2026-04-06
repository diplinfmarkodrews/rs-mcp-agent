namespace RSChatApp.Application.Core.Chat.Commands;

public record RejectToolResultCommand(
    Guid SessionId,
    Guid ToolCallDocumentId,
    string? Reason = null);

