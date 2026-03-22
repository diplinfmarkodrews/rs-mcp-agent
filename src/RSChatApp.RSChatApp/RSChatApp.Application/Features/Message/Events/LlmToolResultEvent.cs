using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.Message.Events;

public record LlmToolResultEvent(
    Guid RequestId,
    Guid SessionId,
    UserId UserId,
    string Token
);
    