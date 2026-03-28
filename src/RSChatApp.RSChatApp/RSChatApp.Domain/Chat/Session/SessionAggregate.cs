using RSChatApp.Common.Kernel;
using RSChatApp.Domain.Chat.Session.Events;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Domain.Chat.Session;

public class SessionAggregate : BaseAggregate
{
    public UserId UserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public Guid? ParentSessionId { get; private set; }

    public string? Summary { get; private set; }

    public Rating? Rating { get; private set; }

    public DateTime StartedAt { get; private set; }

    public DateTime LastActivityAt { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public SessionAggregate()
    {
    }

    public static SessionAggregate Create(Guid id, UserId userId, Guid? parentSessionId = null)
    {
        if (id == Guid.Empty) throw new DomainException("Session id cannot be empty.");
        if (!userId.IsInitialized()) throw new DomainException("User id cannot be empty.");

        var aggregate = new SessionAggregate();

        var @event = SessionCreatedEvent.Create(id, userId, parentSessionId);

        aggregate.ApplyAndEnqueue(@event, e => aggregate.Apply((SessionCreatedEvent)e));

        return aggregate;
    }

    public void UpdateActivity(DateTime lastActivityAt)
    {
        if (lastActivityAt == default) throw new DomainException("Last activity time is required.");

        var @event = new SessionUpdatedEvent(Id, lastActivityAt);

        ApplyAndEnqueue(@event, e => Apply((SessionUpdatedEvent)e));
    }

    public void UpdateSummary(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary)) throw new DomainException("Summary cannot be empty.");

        var @event = new SessionUpdatedEvent(Id, DateTime.UtcNow, Summary: summary);

        ApplyAndEnqueue(@event, e => Apply((SessionUpdatedEvent)e));
    }

    public void UpdateRating(Rating rating)
    {
        var @event = new SessionUpdatedEvent(Id, DateTime.UtcNow, Rating: rating);

        ApplyAndEnqueue(@event, e => Apply((SessionUpdatedEvent)e));
    }

    public void Delete()
    {
        var @event = new SessionDeletedEvent(Id);

        ApplyAndEnqueue(@event, e => Apply((SessionDeletedEvent)e));
    }

    private void Apply(SessionCreatedEvent @event)
    {
        Id = @event.Id;
        UserId = @event.UserId;
        ParentSessionId = @event.ParentSessionId;
        StartedAt = @event.StartedAt;
        LastActivityAt = @event.LastActivityAt;

        Version++;
    }

    private void Apply(SessionUpdatedEvent @event)
    {
        LastActivityAt = @event.LastActivityAt;
        if (@event.Title != null)
            Title = @event.Title;
        if (@event.Summary != null)
            Summary = @event.Summary;
        if (@event.Rating != null)
            Rating = @event.Rating;

        Version++;
    }

    private void Apply(SessionDeletedEvent @event)
    {
        LastActivityAt = DateTime.UtcNow;
        DeletedAt = DateTime.UtcNow;

        Version++;
    }
}
