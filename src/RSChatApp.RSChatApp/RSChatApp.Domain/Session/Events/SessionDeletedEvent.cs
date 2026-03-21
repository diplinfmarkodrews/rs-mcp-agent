using RSChatApp.Common.Kernel;

namespace RSChatApp.Domain.Session.Events;

public record SessionDeletedEvent(Guid Id) : BaseEvent;
