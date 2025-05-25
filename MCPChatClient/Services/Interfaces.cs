using MCPServer.Protos;
using MCPChatClient.Models;

namespace MCPChatClient.Services;

/// <summary>
/// Interface for chat services
/// </summary>
public interface IChatService
{
    Task<ChatServiceResponse> SendMessageAsync(string message, ChatConversation? conversation = null);
    Task<IAsyncEnumerable<string>> SendMessageStreamAsync(string message, ChatConversation? conversation = null);
    Task<bool> IsHealthyAsync();
}

/// <summary>
/// Interface for MCP client service
/// </summary>
public interface IMCPClientService
{
    Task<ChatServiceResponse> SendChatRequestAsync(ChatRequest request);
    Task<IAsyncEnumerable<string>> SendStreamingChatRequestAsync(ChatRequest request);
    Task<bool> CheckHealthAsync();
    Task<MCPServiceResponse> GetServerStatusAsync();
}

/// <summary>
/// Response from MCP service operations
/// </summary>
public class MCPServiceResponse
{
    public bool IsHealthy { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
}
