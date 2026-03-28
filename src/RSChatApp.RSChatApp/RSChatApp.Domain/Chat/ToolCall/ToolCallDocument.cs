using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Domain.Chat.ToolCall;

public record ToolCallDocument
{
    public Guid Id { get; init; }

    public string CallId { get; init; } = string.Empty;

    public Guid MessageId { get; init; }

    public Guid SessionId { get; init; }

    public string ToolName { get; init; } = string.Empty;

    public Dictionary<string, object> Arguments { get; init; } = new();

    public ToolCallStatus Status { get; init; } = ToolCallStatus.Requested;

    public string? Result { get; init; }

    public bool IsError { get; init; }

    public DateTime RequestedAt { get; init; }

    public DateTime? CompletedAt { get; init; }
}
