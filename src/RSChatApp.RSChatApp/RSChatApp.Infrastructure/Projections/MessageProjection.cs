using Marten.Events.Projections;
using RSChatApp.Application.Core.Message.Dtos;
using RSChatApp.Domain.Chat.Message.Events;

namespace RSChatApp.Infrastructure.Projections;

public class MessageProjection : EventProjection
{
    public MessageProjection()
    {
        Project<MessageCreatedEvent>((e, operations) =>
        {
            operations.Store(new MessageDto
            {
                Id = e.Id,
                SessionId = e.SessionId,
                SenderId = e.SenderId,
                Content = e.Content,
                Role = e.Role,
                MessageType = e.MessageType,
                ModelSettingsId = e.ModelSettingsId,
                SentAt = e.SentAt
            });
        });
    }
}
