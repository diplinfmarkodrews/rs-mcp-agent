using RSChatApp.Common.Kernel;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Domain.Chat.Session.Events;

public record SessionCreatedEvent : BaseEvent
{
    public Guid Id { get; init; }

    public UserId UserId { get; init; }

    public Guid? ParentSessionId { get; init; }

    public DateTime StartedAt { get; init; }

    public DateTime LastActivityAt { get; init; }

    public static SessionCreatedEvent Create(Guid id, UserId userId, Guid? parentSessionId = null)
    {
        if (id == Guid.Empty) throw new DomainException("Session id cannot be empty.");
        if (userId.IsInitialized() == false) throw new DomainException("User id cannot be empty.");

        var now = DateTime.UtcNow;

        return new SessionCreatedEvent
        {
            Id = id,
            UserId = userId,
            ParentSessionId = parentSessionId,
            StartedAt = now,
            LastActivityAt = now
        };
    }
}
