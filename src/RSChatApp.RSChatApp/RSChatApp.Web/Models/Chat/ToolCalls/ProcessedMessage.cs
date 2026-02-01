namespace RSChatApp.Web.Models.Chat.ToolCalls;

public record ProcessedMessage(
    Microsoft.Extensions.AI.ChatMessage OriginalMessage,
    string TextContent,
    List<ToolGroup> ToolGroups
);
