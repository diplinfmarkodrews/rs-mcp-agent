using Marten.Events.Aggregation;
using RSChatApp.Application.Core.Message.Dtos;
using RSChatApp.Domain.Chat.Message.Events;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Infrastructure.Persistence.Projections;

public class MessageProjection : SingleStreamProjection<MessageDto, Guid>
{
    public static MessageDto Create(MessageCreatedEvent e) =>
        new()
        {
            Id = e.Id,
            SessionId = e.SessionId,
            SenderId = e.SenderId,
            Content = e.Content,
            Role = e.Role,
            MessageType = e.MessageType,
            ModelSettingsId = e.ModelSettingsId,
            SentAt = e.SentAt,
            IsComplete = e.Role == ChatRole.User // user messages are complete on creation
        };

    public static MessageDto Apply(MessageUpdatedEvent e, MessageDto current) =>
        current with { Content = (current.Content ?? string.Empty) + e.TextDelta };

    public static MessageDto Apply(MessageCompletedEvent e, MessageDto current) =>
        current with
        {
            MessageType = e.MessageType,
            ChatMessageId = e.ChatMessageId,
            AuthorName = e.AuthorName,
            IsComplete = true
        };
}
