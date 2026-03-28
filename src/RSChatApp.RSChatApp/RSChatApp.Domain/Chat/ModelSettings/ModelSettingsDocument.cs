using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Domain.Chat.ModelSettings;

public record ModelSettingsDocument
{
    public Guid Id { get; init; }

    public Guid SessionId { get; init; }

    public string ServiceId { get; init; } = string.Empty;

    public string? ModelId { get; init; }

    public bool IsPrivate { get; init; }

    public AiChatPromptExecutionSettings? ExecutionSettings { get; init; }

    public DateTime CreatedAt { get; init; }

    // Debug method, already filtered by SessionId
    public bool EquivalentTo(ModelSettingsDocument other)
        => ServiceId == other.ServiceId
           && ModelId == other.ModelId
           && IsPrivate == other.IsPrivate
           && ExecutionSettings == other.ExecutionSettings;
}


