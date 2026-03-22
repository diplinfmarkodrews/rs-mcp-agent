using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.Message.Events;

public record LlmToolCallEvent(
    Guid RequestId,
    Guid SessionId,
    UserId UserId,
    string ToolCall
);
