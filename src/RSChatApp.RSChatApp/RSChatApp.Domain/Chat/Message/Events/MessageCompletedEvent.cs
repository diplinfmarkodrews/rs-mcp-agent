using RSChatApp.Common.Kernel;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Domain.Chat.Message.Events;

public record MessageCompletedEvent(
    MessageType MessageType,
    string? ChatMessageId,
    string? AuthorName) : BaseEvent;

