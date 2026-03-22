using RSChatApp.Common.Kernel;
using RSChatApp.Domain.Message.Events;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Domain.Message;

public class MessageAggregate : BaseAggregate
{
    public UserId SenderId { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public ChatRole Role { get; set; }

    public DateTime SentAt { get; private set; }

    public MessageAggregate()
    {
    }

    public static MessageAggregate Create(Guid id, Guid sessionId, UserId senderId, string content, ChatRole role)
    {
        if (id == Guid.Empty) throw new DomainException("Message id cannot be empty.");
        if (senderId.IsInitialized() == false) throw new DomainException("Sender id cannot be empty.");
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Content cannot be empty.");

        var message = new MessageAggregate();

        var @event = MessageCreatedEvent.Create(id, sessionId, senderId, content, role);
        message.ApplyAndEnqueue(@event, e => message.Apply((MessageCreatedEvent)e));

        return message;
    }

    private void Apply(MessageCreatedEvent @event)
    {
        Id = @event.Id;
        SenderId = @event.SenderId;
        Content = @event.Content;
        Role = @event.Role;
        SentAt = @event.SentAt;

        Version++;
    }
}
