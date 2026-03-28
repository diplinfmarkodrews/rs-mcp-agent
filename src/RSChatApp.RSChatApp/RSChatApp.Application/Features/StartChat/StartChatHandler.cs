using RSChatApp.Domain.Chat.Session;

namespace RSChatApp.Application.Features.StartChat;

public static class StartChatHandler
{
    public static IEnumerable<object> Handle(
        StartChatCommand command,
        IEventStoreRepository<SessionAggregate> repository)
    {
        var aggregate = SessionAggregate.Create(command.Id, command.UserId, command.ParentSessionId);

        repository.Save(aggregate);

        return aggregate.DequeueUncommittedEvents();
    }
}
