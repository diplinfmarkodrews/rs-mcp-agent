namespace RSChatApp.Application.Core.Chat.Commands;

public record ConfirmToolCallCommand(
    Guid SessionId,
    Guid ToolCallDocumentId,
    IDictionary<string, object?>? EditedArguments = null);

