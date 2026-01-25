using Microsoft.Extensions.AI;

namespace RSChatApp.Web.Models.Chat;

public static class ChatMessageExtension
{
    /// <summary>
    /// Normalizes messages for the API by splitting assistant messages that contain both FunctionCallContent and FunctionResultContent.
    /// Claude API requires: Assistant (with tool calls) -> Tool (with results) -> Assistant (with response)
    /// But we store them combined for display purposes.
    /// </summary>
    public static List<ChatMessage> NormalizeMessagesForApi(this List<ChatMessage> messages)
    {
        var normalized = new List<ChatMessage>();
        
        foreach (var message in messages)
        {
            // Only process assistant messages that might have tool calls
            if (message.Role != ChatRole.Assistant || message.Contents is null || message.Contents.Count <= 1)
            {
                normalized.Add(message);
                continue;
            }
            
            // Check if this message has both FunctionCallContent and FunctionResultContent
            var functionCalls = message.Contents.OfType<FunctionCallContent>().ToList();
            var functionResults = message.Contents.OfType<FunctionResultContent>().ToList();
            
            if (functionCalls.Count == 0 || functionResults.Count == 0)
            {
                // No splitting needed - either no tool calls or they're already separate
                normalized.Add(message);
                continue;
            }
            
            // Split into multiple messages:
            // 1. Assistant message with text (if any) + FunctionCallContent
            var assistantContents = new List<AIContent>();
            var textContent = message.Contents.OfType<TextContent>().FirstOrDefault();
            if (textContent != null && !string.IsNullOrWhiteSpace(textContent.Text))
            {
                assistantContents.Add(textContent);
            }
            assistantContents.AddRange(functionCalls.Cast<AIContent>());
            
            if (assistantContents.Count > 0)
            {
                normalized.Add(new ChatMessage(ChatRole.Assistant, assistantContents));
            }
            
            // 2. Tool message(s) with FunctionResultContent
            foreach (var result in functionResults)
            {
                normalized.Add(new ChatMessage(ChatRole.Tool, new List<AIContent> { result }));
            }
        }
        
        return normalized;
    }
}