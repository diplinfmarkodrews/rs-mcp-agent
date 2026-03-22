using RSChatApp.Domain.Message;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.SendMessage;

public static class SendMessageHandler
{
    public static IEnumerable<object> Handle(
        SendMessageCommand command,
        IEventStoreRepository<MessageAggregate> repository)
    {
        var aggregate = MessageAggregate.Create(command.Id, command.SessionId, command.SenderId, command.Content, ChatRole.User);

        repository.Save(aggregate);

        return aggregate.DequeueUncommittedEvents();
    }
}
