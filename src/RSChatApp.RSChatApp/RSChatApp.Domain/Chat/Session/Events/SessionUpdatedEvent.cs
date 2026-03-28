using RSChatApp.Common.Kernel;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Domain.Chat.Session.Events;

public record SessionUpdatedEvent(Guid Id, DateTime LastActivityAt, string? Title = null, string? Summary = null, Rating? Rating = null) : BaseEvent;
