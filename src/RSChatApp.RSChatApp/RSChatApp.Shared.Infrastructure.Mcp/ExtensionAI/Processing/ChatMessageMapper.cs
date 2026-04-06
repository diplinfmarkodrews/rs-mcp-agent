
using Microsoft.Extensions.AI;
using RSChatApp.Application.Core.Chat.Dtos;

namespace RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.Processing;

public static class ChatMessageMapper
{
    public static List<ChatMessage> ToChatMessageList(this IEnumerable<ChatMessageDto> chatMessageDtos)
    {
        List<ChatMessage> result = new();
        var groupByMsgId = chatMessageDtos.GroupBy(dto => dto.ChatMessageId);
        foreach (var msgGrp in groupByMsgId)  
        {
            // var textMsg = msgGrp.Where(dto => dto. == ChatMessageContentType.Text).FirstOrDefault();
            // var msg = new ChatMessage(first.Id, first.Role.ToChatRole(), contents);
            // msg.MessageId = first.ChatMessageId;
        }
            
        return result;
    }
}