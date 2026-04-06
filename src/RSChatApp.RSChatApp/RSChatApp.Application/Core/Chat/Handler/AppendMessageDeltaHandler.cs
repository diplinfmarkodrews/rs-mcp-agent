using RSChatApp.Application.Core.Chat.Events;
using RSChatApp.Domain.Chat.Message;

namespace RSChatApp.Application.Core.Chat.Handler;

public static class AppendMessageDeltaHandler
{
    public static async Task Handle(
        LlmTokenGeneratedEvent message,
        IEventStoreRepository<MessageAggregate> repository,
        CancellationToken ct)
    {
        var aggregate = await repository.LoadAsync(message.MessageId, 0, ct);
        if (aggregate is null) return;

        aggregate.AppendDelta(message.Token);
        repository.Save(aggregate);
    }
}

