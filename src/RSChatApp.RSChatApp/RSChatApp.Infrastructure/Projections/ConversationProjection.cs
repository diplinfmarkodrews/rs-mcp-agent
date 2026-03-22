using Marten.Events.Projections;
using RSChatApp.Application.Core.Message.Dtos;

namespace RSChatApp.Infrastructure.Projections;

public class ConversationProjection : MultiStreamProjection<ConversationDto, Guid>
{
    public ConversationProjection()
    {
        CreateEvent<SessionCreatedEvent>(e => ConversationProjectionLogic.Handle(e)!);
        ProjectEvent<MessageCreatedEvent>((c, e) => ConversationProjectionLogic.Handle(e, c)!);
        ProjectEvent<SessionDeletedEvent>((c, e) => ConversationProjectionLogic.Handle(c)!);

        Identity<SessionCreatedEvent>(x => x.Id);
        Identity<MessageCreatedEvent>(x => x.SessionId);
        Identity<SessionDeletedEvent>(x => x.Id);
    }
}
