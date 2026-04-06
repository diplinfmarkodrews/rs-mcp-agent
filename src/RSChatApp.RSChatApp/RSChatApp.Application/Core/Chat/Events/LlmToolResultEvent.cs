using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Core.Chat.Events;

public record LlmToolResultEvent(
    Guid RequestId,
    Guid SessionId,
    Guid MessageId,
    UserId UserId,
    bool IsLocal,
    string ToolCallId,
    object ToolResult);
    