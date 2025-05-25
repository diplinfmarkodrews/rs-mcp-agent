using MCPServer.Protos;

namespace MCPChatClient.Models;

/// <summary>
/// Configuration settings for the ChatAI service
/// </summary>
public class ChatAISettings
{
    public string Model { get; set; } = "gpt-3.5-turbo";
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 1000;
    public string SystemMessage { get; set; } = "You are a helpful AI assistant.";
}

/// <summary>
/// Configuration settings for the MCP Server connection
/// </summary>
public class MCPServerSettings
{
    public string Address { get; set; } = "http://localhost:5000";
}

/// <summary>
/// Represents a chat conversation
/// </summary>
public class ChatConversation
{
    public List<ChatMessage> Messages { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ConversationId { get; set; }
}

/// <summary>
/// Response from the chat service with additional metadata
/// </summary>
public class ChatServiceResponse
{
    public string Content { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string FinishReason { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public bool IsFromMCP { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
