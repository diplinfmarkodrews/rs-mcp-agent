using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Core.Message.Dtos;

public record ConversationDto
{
    public Guid Id { get; init; }

    public UserId UserId { get; init; }

    public string Title { get; init; } = string.Empty;

    public Guid? ParentSessionId { get; init; }

    public string? Summary { get; init; }

    public Rating? Rating { get; init; }

    public DateTime StartedAt { get; init; }

    public DateTime LastActivityAt { get; init; }

    public bool Closed { get; init; }

    public long Version { get; init; }
}
