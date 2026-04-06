using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Domain.Chat.ModelSettings;

public record ModelSettingsDocument
{
    public Guid Id { get; init; }

    public Guid SessionId { get; init; }

    public string ServiceId { get; init; } = string.Empty;

    public string? ModelId { get; init; }

    public bool IsPrivate { get; init; }

    public IReadOnlyList<string> ActiveToolNames { get; init; } = [];

    public AiChatPromptExecutionSettings? ExecutionSettings { get; init; }

    public DateTime CreatedAt { get; init; }
    
    // Debug method, already filtered by SessionId
    public bool EquivalentTo(ModelSettingsDocument other)
        => ServiceId == other.ServiceId
           && ModelId == other.ModelId
           && IsPrivate == other.IsPrivate
           && ActiveToolNames.Order().SequenceEqual(other.ActiveToolNames.Order())
           && ExecutionSettings == other.ExecutionSettings;

    public static ModelSettingsDocument Create(Guid sessionId, string serviceId, string modelId, bool isPrivate,
        IEnumerable<string> activeToolNames, AiChatPromptExecutionSettings executionSettings)
        => new ModelSettingsDocument
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            ServiceId = serviceId,
            ModelId = modelId,
            IsPrivate = isPrivate,
            ActiveToolNames = activeToolNames.ToList(),
            ExecutionSettings = executionSettings,
            CreatedAt = DateTime.UtcNow
        };
}


