using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Domain.Chat.ToolCall;

public record ToolCallDocument
{
    public Guid Id { get; init; }

    public string CallId { get; private set; } = string.Empty;
    
    public Guid MessageId { get; init; }

    public Guid SessionId { get; init; }

    public string ToolName { get; init; } = string.Empty;

    public Dictionary<string, object> Arguments { get; init; } = new();
    
    public ToolCallStatus Status { get; init; }

    public object? Result { get; init; }

    public bool IsError { get; init; }

    public DateTime RequestedAt { get; init; }

    public DateTime? CompletedAt { get; init; }
    
    public static ToolCallDocument Create(string callId, Guid messageId, Guid sessionId, string toolName, Dictionary<string, object> arguments)
        => new ToolCallDocument
        {
            Id = Guid.NewGuid(),
            CallId = callId,
            MessageId = messageId,
            SessionId = sessionId,
            ToolName = toolName,
            Arguments = arguments,
            Status = ToolCallStatus.Requested,
            RequestedAt = DateTime.UtcNow
        };
}
