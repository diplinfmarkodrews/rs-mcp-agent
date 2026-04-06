using RSChatApp.Common.Kernel;

namespace RSChatApp.Domain.Chat.Message.Events;

public record MessageUpdatedEvent(string TextDelta) : BaseEvent;

