using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Core.Chat.Events;

public record LlmToolCallEvent(
    Guid RequestId,
    Guid SessionId,
    Guid MessageId,
    UserId UserId,
    string ToolName,
    IReadOnlyDictionary<string, object> Arguments
);
