

using RSChatApp.Application.Core.Message.Dtos;

namespace RSChatApp.Infrastructure.Projections;

public static class ConversationProjectionLogic
{
    public static ConversationDto? Handle(SessionCreatedEvent @event)
    {
        return new ConversationDto
        {
            Id = @event.Id,
            UserId = @event.UserId,
            Title = @event.Title,
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