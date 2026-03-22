using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.Message.Events;

public record LlmResponseGaveUpEvent(
    Guid RequestId,
    Guid SessionId,
    UserId UserId,
    GaveUpReasons Reason);