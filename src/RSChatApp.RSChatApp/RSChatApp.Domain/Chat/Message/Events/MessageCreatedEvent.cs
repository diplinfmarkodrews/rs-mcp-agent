using RSChatApp.Common.Kernel;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Domain.Chat.Message.Events;

public record MessageCreatedEvent : BaseEvent
{
    public Guid Id { get; init; }

    public DateTime CreatedAt { get; set; }

    public UserId SenderId { get; private set; }

    public Guid SessionId { get; private set; }

    public string? Content { get; private set; }
    
    public string? ChatMessageId { get; private set; }
    
    public string? AuthorName { get; private set; }

    public required ChatRole Role { get; init; }

    public MessageType MessageType { get; private set; }

    public Guid? ModelSettingsId { get; private set; }

    public DateTime SentAt { get; private set; }
   

    public static MessageCreatedEvent Create(Guid id, Guid sessionId, UserId senderId, string? content, ChatRole role, MessageType messageType, string? chatMessageId, string? authorName, Guid? modelSettingsId = null)
    {
        if (id == Guid.Empty) throw new DomainException("Message id cannot be empty.");
        if (sessionId == Guid.Empty) throw new DomainException("Session id cannot be empty.");
        if (role == null) throw new DomainException("Role cannot be empty.");
        if (role == ChatRole.User && string.IsNullOrWhiteSpace(content))
            throw new DomainException("User message content cannot be empty.");

        return new MessageCreatedEvent
        {
            Id = id,
            SessionId = sessionId,
            SenderId = senderId,
            Content = content,
            Role = role,
            MessageType = messageType,
            ChatMessageId = chatMessageId,
            AuthorName = authorName,
            ModelSettingsId = modelSettingsId,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}
