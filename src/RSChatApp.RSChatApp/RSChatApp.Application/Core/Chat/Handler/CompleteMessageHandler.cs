using RSChatApp.Application.Core.Chat.Events;
using RSChatApp.Domain.Chat.Message;

namespace RSChatApp.Application.Core.Chat.Handler;

public static class CompleteMessageHandler
{
    public static async Task Handle(
        LlmResponseCompletedEvent message,
        IEventStoreRepository<MessageAggregate> repository,
        CancellationToken ct)
    {
        var aggregate = await repository.LoadAsync(message.MessageId, 0, ct);
        if (aggregate is null) return;

        aggregate.Complete(message.MessageType, message.ChatMessageId, message.AuthorName);
        repository.Save(aggregate);
    }
}

