using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Core.Message.Dtos;

public record MessageDto
{
    public Guid Id { get; init; }

    public Guid SessionId { get; init; }

    public UserId SenderId { get; init; }

    public string? Content { get; init; }

    public ChatRole Role { get; init; }

    public MessageType MessageType { get; init; }

    public Guid? ModelSettingsId { get; init; }

    public DateTime SentAt { get; init; }

    public long Version { get; init; }
}
