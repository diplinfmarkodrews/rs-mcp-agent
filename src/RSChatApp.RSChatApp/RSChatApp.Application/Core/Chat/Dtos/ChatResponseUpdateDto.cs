namespace RSChatApp.Application.Core.Chat.Dtos;

public record ChatResponseUpdateDto(
    string? TextDelta = null,
    string? FinishReason = null,
    string? Role = null,
    ToolCallInfo? ToolCall = null,
    ToolResultInfo? ToolResult = null);

public record ToolCallInfo(string Name, Dictionary<string, object> Arguments);

public record ToolResultInfo(string CallId, string Result);