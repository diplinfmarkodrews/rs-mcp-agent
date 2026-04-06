using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Core.Chat.Events;

public record LlmResponseRetryingEvent(
    Guid RequestId,
    Guid SessionId,
    UserId UserId
);