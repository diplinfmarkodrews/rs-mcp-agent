using RSChatApp.Application.Core.Message.Dtos;
using RSChatApp.Domain.Chat.Message.Events;
using RSChatApp.Domain.Chat.Session.Events;

namespace RSChatApp.Infrastructure.Persistence.Projections;

public static class ConversationProjectionLogic
{
    public static ConversationDto? Handle(SessionCreatedEvent @event)
    {
        return new ConversationDto
        {
            Id = @event.Id,
            UserId = @event.UserId,
            ParentSessionId = @event.ParentSessionId,
            StartedAt = @event.StartedAt,
            LastActivityAt = @event.LastActivityAt
        };
    }

    public static ConversationDto? Handle(MessageCreatedEvent @event, ConversationDto? current)
    {
        if (current is null) return null;

        return current with
        {
            LastActivityAt = @event.SentAt
        };
    }

    public static ConversationDto? Handle(SessionUpdatedEvent @event, ConversationDto? current)
    {
        if (current is null) return null;

        var updated = current with { LastActivityAt = @event.LastActivityAt };

        if (@event.Title != null)
            updated = updated with { Title = @event.Title };
        
        if (@event.Summary != null)
            updated = updated with { Summary = @event.Summary };
        
        if (@event.Rating != null)
            updated = updated with { Rating = @event.Rating };

        return updated;
    }

    public static ConversationDto? Handle(ConversationDto? current)
    {
        if (current is null) return null;

        return current with
        {
            LastActivityAt = DateTime.UtcNow,
            Closed = true
        };
    }
}