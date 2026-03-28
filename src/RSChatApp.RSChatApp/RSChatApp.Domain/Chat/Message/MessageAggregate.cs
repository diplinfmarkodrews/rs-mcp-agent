using RSChatApp.Common.Kernel;
using RSChatApp.Domain.Chat.Message.Events;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Domain.Chat.Message;

public class MessageAggregate : BaseAggregate
{
    public UserId SenderId { get; private set; }

    public string? Content { get; private set; }

    public ChatRole Role { get; set; }

    public MessageType MessageType { get; private set; }

    public Guid? ModelSettingsId { get; private set; }

    public DateTime SentAt { get; private set; }

    public MessageAggregate()
    {
    }

    public static MessageAggregate Create(Guid id, Guid sessionId, UserId senderId, string? content, ChatRole role, MessageType messageType, Guid? modelSettingsId = null)
    {
        if (id == Guid.Empty) throw new DomainException("Message id cannot be empty.");
        if (senderId.IsInitialized() == false) throw new DomainException("Sender id cannot be empty.");

        var message = new MessageAggregate();

        var @event = MessageCreatedEvent.Create(id, sessionId, senderId, content, role, messageType, modelSettingsId);
        message.ApplyAndEnqueue(@event, e => message.Apply((MessageCreatedEvent)e));

        return message;
    }

    private void Apply(MessageCreatedEvent @event)
    {
        Id = @event.Id;
        SenderId = @event.SenderId;
        Content = @event.Content;
        Role = @event.Role;
        MessageType = @event.MessageType;
        ModelSettingsId = @event.ModelSettingsId;
        SentAt = @event.SentAt;

        Version++;
    }
}
