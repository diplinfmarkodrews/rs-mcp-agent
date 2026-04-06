using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Core.Chat.Events;

public record LlmResponseCompletedEvent(
    Guid RequestId,
    Guid SessionId,
    Guid MessageId,
    UserId UserId,
    MessageType MessageType,
    string? ChatMessageId,
    string? AuthorName
);
