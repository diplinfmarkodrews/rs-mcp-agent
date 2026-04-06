using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Core.Chat.Events;

public record ToolCallConfirmationRequestedEvent(
    Guid SessionId,
    Guid ToolCallDocumentId,
    string ToolName,
    IReadOnlyDictionary<string, object> Arguments);

