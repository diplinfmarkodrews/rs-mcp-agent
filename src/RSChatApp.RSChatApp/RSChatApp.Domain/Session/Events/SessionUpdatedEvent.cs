using RSChatApp.Common.Kernel;

namespace RSChatApp.Domain.Session.Events;

public record SessionUpdatedEvent(Guid Id, DateTime LastActivityAt) : BaseEvent;
