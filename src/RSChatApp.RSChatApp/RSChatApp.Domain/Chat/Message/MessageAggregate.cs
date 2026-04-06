using RSChatApp.Common.Kernel;
using RSChatApp.Domain.Chat.Message.Events;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Domain.Chat.Message;

public class MessageAggregate : BaseAggregate
{
    public UserId SenderId { get; private set; }

    public string? Content { get; private set; }

    public ChatRole? Role { get; private set; }

    public MessageType MessageType { get; private set; }

    public string? ChatMessageId { get; private set; }
    
    public string? AuthorName { get; private set; }
    
    public Guid? ModelSettingsId { get; private set; }

    public DateTime SentAt { get; private set; }

    public static MessageAggregate Create(Guid id, Guid sessionId, UserId senderId, string? content, ChatRole role, MessageType messageType, string? chatMessageId, string? authorName, Guid? modelSettingsId = null)
    {
        if (id == Guid.Empty) throw new DomainException("Message id cannot be empty.");
        
        var message = new MessageAggregate();

        var @event = MessageCreatedEvent.Create(id, sessionId, senderId, content, role, messageType, chatMessageId, authorName, modelSettingsId);
        message.ApplyAndEnqueue(@event, e => message.Apply((MessageCreatedEvent)e));

        return message;
    }

    public void AppendDelta(string textDelta)
    {
        var @event = new MessageUpdatedEvent(textDelta);
        ApplyAndEnqueue(@event, e => Apply((MessageUpdatedEvent)e));
    }

    public void Complete(MessageType messageType, string? chatMessageId, string? authorName)
    {
        var @event = new MessageCompletedEvent(messageType, chatMessageId, authorName);
        ApplyAndEnqueue(@event, e => Apply((MessageCompletedEvent)e));
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

    private void Apply(MessageUpdatedEvent @event)
    {
        Content = (Content ?? string.Empty) + @event.TextDelta;
        Version++;
    }

    private void Apply(MessageCompletedEvent @event)
    {
        MessageType = @event.MessageType;
        ChatMessageId = @event.ChatMessageId;
        AuthorName = @event.AuthorName;
        Version++;
    }
}
