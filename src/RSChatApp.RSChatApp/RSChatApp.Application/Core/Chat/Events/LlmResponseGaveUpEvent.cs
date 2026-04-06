using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Core.Chat.Events;

public record LlmResponseGaveUpEvent(
    Guid RequestId,
    Guid SessionId,
    UserId UserId,
    GaveUpReasons Reason);