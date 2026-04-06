namespace RSChatApp.Application.Core.Chat.Events;

public record ToolResultConfirmationRequestedEvent(
    Guid SessionId,
    Guid ToolCallDocumentId,
    string CallId,
    string ToolName,
    object Result);

