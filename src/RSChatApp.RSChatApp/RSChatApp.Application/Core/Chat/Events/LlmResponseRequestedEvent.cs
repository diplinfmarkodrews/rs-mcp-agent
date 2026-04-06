using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Core.Chat.Events;

public record LlmResponseRequestedEvent(
     Guid RequestId,
     Guid SessionId,
     Guid MessageId,
     UserId UserId,
     AiChatRequest AiChatRequest
);
