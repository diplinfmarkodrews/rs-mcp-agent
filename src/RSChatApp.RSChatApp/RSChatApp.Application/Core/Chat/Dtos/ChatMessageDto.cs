using RSChatApp.Application.Core.Message.Dtos;
using RSChatApp.Domain.Chat.ToolCall;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Core.Chat.Dtos;

public record ChatMessageDto(ChatRole Role, string Content)
{
    public IEnumerable<ToolCallDocument>? ToolCalls { get; init; }
    public string? ChatMessageId { get; init; }
    public string? AuthorName { get; init; }
    public MessageType MessageType { get; init; }
}

public static class ChatMessageDtoExtensions
{
    public static ChatMessageDto ToChatMessageDto(this MessageDto msg, IEnumerable<ToolCallDocument>? toolCalls = null) =>
        new(msg.Role, msg.Content ?? string.Empty)
        {
            ChatMessageId = msg.ChatMessageId,
            AuthorName = msg.AuthorName,
            MessageType = msg.MessageType,
            ToolCalls = toolCalls
        };

    public static IReadOnlyList<ChatMessageDto> ToChatMessageDtos(
        this IEnumerable<MessageDto> messages,
        IEnumerable<ToolCallDocument> toolCalls)
    {
        var toolCallsByMessageId = toolCalls
            .GroupBy(tc => tc.MessageId)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());

        return messages
            .Select(msg => msg.ToChatMessageDto(
                toolCallsByMessageId.GetValueOrDefault(msg.Id)))
            .ToList();
    }
}
