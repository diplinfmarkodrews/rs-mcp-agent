using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Core.Chat.Events;

public record LlmTokenGeneratedEvent(
    Guid RequestId,
    Guid SessionId,
    Guid MessageId,
    UserId UserId,
    string Token
);
