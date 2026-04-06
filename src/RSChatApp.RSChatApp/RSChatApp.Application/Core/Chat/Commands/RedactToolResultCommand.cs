namespace RSChatApp.Application.Core.Chat.Commands;

public record RedactToolResultCommand(
    Guid SessionId,
    Guid ToolCallDocumentId,
    object RedactedResult);

