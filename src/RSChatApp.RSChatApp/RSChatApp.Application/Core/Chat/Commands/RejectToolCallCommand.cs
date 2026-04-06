namespace RSChatApp.Application.Core.Chat.Commands;

public record RejectToolCallCommand(
    Guid SessionId,
    Guid ToolCallDocumentId,
    string? Reason = null);

