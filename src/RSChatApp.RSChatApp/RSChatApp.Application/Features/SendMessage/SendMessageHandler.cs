using RSChatApp.Domain.Chat.Message;

namespace RSChatApp.Application.Features.SendMessage;

public static class SendMessageHandler
{
    public static IEnumerable<object> Handle(
        SendMessageCommand command,
        IEventStoreRepository<MessageAggregate> repository)
    {
        var aggregate = MessageAggregate.Create(
            command.Id,
            command.SessionId,
            command.SenderId,
            command.Content,
            command.Role,
            command.MessageType,
            command.ModelSettingsId);

        repository.Save(aggregate);

        return aggregate.DequeueUncommittedEvents();
    }
}
