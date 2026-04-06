namespace RSChatApp.Application.Core.Chat.Commands;

public record ConfirmToolResultCommand(
    Guid SessionId,
    Guid ToolCallDocumentId);

