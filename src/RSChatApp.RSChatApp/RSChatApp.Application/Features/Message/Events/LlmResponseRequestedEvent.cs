using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.Message.Events;

public record LlmResponseRequestedEvent(
     Guid RequestId,
     Guid SessionId,
     UserId UserId,
     IReadOnlyList<ChatTurn> Messages
);
