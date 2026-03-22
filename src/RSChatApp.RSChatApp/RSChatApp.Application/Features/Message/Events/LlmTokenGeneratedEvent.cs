using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.Message.Events;

public record LlmTokenGeneratedEvent(
    Guid RequestId,
    Guid SessionId,
    UserId UserId,
    string Token
);
