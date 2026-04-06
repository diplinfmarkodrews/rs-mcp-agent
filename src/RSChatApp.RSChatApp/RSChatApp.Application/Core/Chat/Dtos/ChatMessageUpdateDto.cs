using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Core.Chat.Dtos;

public record ChatMessageUpdateDto(
    string? TextDelta = null,
    string? FinishReason = null,
    string? ChatMessageId = null,
    string? AuthorName = null,
    ChatRole Role = null,
    ToolCallInfo? ToolCall = null,
    ToolResultInfo? ToolResult = null);

public record ToolCallInfo(string Name, Dictionary<string, object> Arguments);

public record ToolResultInfo(string CallId, object Result);