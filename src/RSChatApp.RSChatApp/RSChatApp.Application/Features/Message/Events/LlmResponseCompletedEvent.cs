using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.Message.Events;

public record LlmResponseCompletedEvent(
    Guid RequestId,
    Guid SessionId,
    UserId UserId,
    string FullResponse,
    int TokenCount
);
