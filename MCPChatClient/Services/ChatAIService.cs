using MCPServer.Protos;
using MCPChatClient.Models;
using MCPChatClient.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MCPChatClient.Services;

/// <summary>
/// Intelligent chat service that routes requests between local AI processing and MCP server
/// </summary>
public class ChatAIService : IChatService
{
    private readonly ILogger<ChatAIService> _logger;
    private readonly IMCPClientService _mcpClient;
    private readonly ChatAISettings _settings;

    public ChatAIService(
        ILogger<ChatAIService> logger,
        IMCPClientService mcpClient,
        IOptions<ChatAISettings> settings)
    {
        _logger = logger;
        _mcpClient = mcpClient;
        _settings = settings.Value;
    }

    public async Task<ChatServiceResponse> SendMessageAsync(string message, ChatConversation? conversation = null)
    {
        try
        {
            _logger.LogInformation("Processing chat message: {Message}", message);

            // Determine if this is a report-related query that should go to MCP
            if (IsReportRelatedQuery(message))
            {
                _logger.LogDebug("Routing message to MCP server for report processing");
                return await SendToMCPAsync(message, conversation);
            }

            // For non-report queries, we could implement local AI processing here
            // For now, we'll route everything to MCP to demonstrate the integration
            _logger.LogDebug("Routing message to MCP server for general processing");
            return await SendToMCPAsync(message, conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            throw;
        }
    }

    public async Task<IAsyncEnumerable<string>> SendMessageStreamAsync(string message, ChatConversation? conversation = null)
    {
        try
        {
            _logger.LogInformation("Processing streaming chat message: {Message}", message);

            // Create the chat request
            var request = CreateChatRequest(message, conversation);
            
            // Send to MCP for streaming response
            return await _mcpClient.SendStreamingChatRequestAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing streaming chat message");
            throw;
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            return await _mcpClient.CheckHealthAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed");
            return false;
        }
    }

    private async Task<ChatServiceResponse> SendToMCPAsync(string message, ChatConversation? conversation)
    {
        var request = CreateChatRequest(message, conversation);
        return await _mcpClient.SendChatRequestAsync(request);
    }

    private ChatRequest CreateChatRequest(string message, ChatConversation? conversation)
    {
        var request = new ChatRequest
        {
            Model = _settings.Model,
            Temperature = _settings.Temperature,
            MaxTokens = _settings.MaxTokens
        };

        // Add system message
        request.Messages.Add(new ChatMessage
        {
            Role = Role.System,
            Content = _settings.SystemMessage
        });

        // Add conversation history if available
        if (conversation?.Messages != null)
        {
            foreach (var msg in conversation.Messages.TakeLast(10)) // Limit context to last 10 messages
            {
                request.Messages.Add(msg);
            }
        }

        // Add the current user message
        request.Messages.Add(new ChatMessage
        {
            Role = Role.User,
            Content = message
        });

        return request;
    }

    private bool IsReportRelatedQuery(string message)
    {
        var lowerMessage = message.ToLowerInvariant();
        
        // Keywords that indicate report-related queries
        var reportKeywords = new[]
        {
            "report", "generate", "create", "available", "list", "templates",
            "summary", "monthly", "weekly", "daily", "analytics", "data",
            "export", "pdf", "chart", "graph", "statistics", "metrics"
        };

        return reportKeywords.Any(keyword => lowerMessage.Contains(keyword));
    }
}
