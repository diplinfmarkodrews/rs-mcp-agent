using RSChatApp.Common.Kernel;

namespace RSChatApp.Domain.Chat.Session.Events;

public record SessionDeletedEvent(Guid Id) : BaseEvent;
