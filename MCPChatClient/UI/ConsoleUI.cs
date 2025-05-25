using MCPServer.Protos;
using MCPChatClient.Models;
using MCPChatClient.Services;
using Microsoft.Extensions.Logging;

namespace MCPChatClient.UI;

/// <summary>
/// Console-based user interface for the MCP Chat Client
/// </summary>
public class ConsoleUI
{
    private readonly ILogger<ConsoleUI> _logger;
    private readonly IChatService _chatService;
    private readonly IMCPClientService _mcpClient;
    private ChatConversation _currentConversation;

    public ConsoleUI(
        ILogger<ConsoleUI> logger,
        IChatService chatService,
        IMCPClientService mcpClient)
    {
        _logger = logger;
        _chatService = chatService;
        _mcpClient = mcpClient;
        _currentConversation = new ChatConversation
        {
            ConversationId = Guid.NewGuid().ToString()
        };
    }

    public async Task RunAsync()
    {
        DisplayHeader();
        await CheckServerHealth();
        
        while (true)
        {
            try
            {
                var input = GetUserInput();
                
                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (IsExitCommand(input))
                {
                    Console.WriteLine("Goodbye!");
                    break;
                }

                if (IsSpecialCommand(input))
                {
                    await HandleSpecialCommand(input);
                    continue;
                }

                await ProcessChatMessage(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                _logger.LogError(ex, "Error in console UI");
            }
        }
    }

    private void DisplayHeader()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════╗");
        Console.WriteLine("║        MCP Chat Client with AI          ║");
        Console.WriteLine("║     Model Context Protocol Integration  ║");
        Console.WriteLine("╚══════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("Available commands:");
        Console.WriteLine("  /help       - Show this help message");
        Console.WriteLine("  /status     - Check MCP server status");
        Console.WriteLine("  /stream     - Enable streaming mode");
        Console.WriteLine("  /clear      - Clear conversation history");
        Console.WriteLine("  /exit       - Exit the application");
        Console.WriteLine("  /quit       - Exit the application");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  • What reports are available?");
        Console.WriteLine("  • Generate a monthly sales report");
        Console.WriteLine("  • Show me report templates");
        Console.WriteLine("  • Create a summary for May 2025");
        Console.WriteLine();
    }

    private async Task CheckServerHealth()
    {
        Console.Write("🔍 Checking MCP server health... ");
        
        try
        {
            var status = await _mcpClient.GetServerStatusAsync();
            if (status.IsHealthy)
            {
                Console.WriteLine("✅ Connected");
            }
            else
            {
                Console.WriteLine($"⚠️  {status.StatusMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed: {ex.Message}");
        }
        
        Console.WriteLine();
    }

    private string GetUserInput()
    {
        Console.Write("🤖 You: ");
        return Console.ReadLine() ?? string.Empty;
    }

    private bool IsExitCommand(string input)
    {
        var exitCommands = new[] { "/exit", "/quit", "exit", "quit" };
        return exitCommands.Contains(input.ToLowerInvariant());
    }

    private bool IsSpecialCommand(string input)
    {
        return input.StartsWith("/");
    }

    private async Task HandleSpecialCommand(string command)
    {
        switch (command.ToLowerInvariant())
        {
            case "/help":
                DisplayHeader();
                break;

            case "/status":
                await DisplayServerStatus();
                break;

            case "/stream":
                await EnableStreamingMode();
                break;

            case "/clear":
                ClearConversation();
                break;

            default:
                Console.WriteLine("❓ Unknown command. Type /help for available commands.");
                break;
        }
    }

    private async Task DisplayServerStatus()
    {
        Console.WriteLine("📊 MCP Server Status:");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━");
        
        try
        {
            var status = await _mcpClient.GetServerStatusAsync();
            Console.WriteLine($"Health: {(status.IsHealthy ? "✅ Healthy" : "❌ Unhealthy")}");
            Console.WriteLine($"Message: {status.StatusMessage}");
            Console.WriteLine($"Last Checked: {status.LastChecked:yyyy-MM-dd HH:mm:ss} UTC");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to get status: {ex.Message}");
        }
        
        Console.WriteLine();
    }

    private async Task EnableStreamingMode()
    {
        Console.WriteLine("🌊 Streaming Mode Enabled");
        Console.WriteLine("Type your message to see streaming response:");
        Console.Write("🤖 You: ");
        
        var input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input))
        {
            await ProcessStreamingMessage(input);
        }
    }

    private void ClearConversation()
    {
        _currentConversation = new ChatConversation
        {
            ConversationId = Guid.NewGuid().ToString()
        };
        Console.WriteLine("🧹 Conversation history cleared.");
        Console.WriteLine();
    }

    private async Task ProcessChatMessage(string message)
    {
        Console.Write("🤖 Assistant: ");
        
        try
        {
            var response = await _chatService.SendMessageAsync(message, _currentConversation);
            
            Console.WriteLine(response.Content);
            
            // Add to conversation history
            _currentConversation.Messages.Add(new ChatMessage
            {
                Role = Role.User,
                Content = message
            });
            
            _currentConversation.Messages.Add(new ChatMessage
            {
                Role = Role.Assistant,
                Content = response.Content
            });

            // Display metadata if available
            if (response.Metadata.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("📋 Additional Information:");
                foreach (var metadata in response.Metadata)
                {
                    Console.WriteLine($"   {metadata.Key}: {metadata.Value}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
        
        Console.WriteLine();
    }

    private async Task ProcessStreamingMessage(string message)
    {
        Console.Write("🤖 Assistant: ");
        
        try
        {
            var streamResponse = await _chatService.SendMessageStreamAsync(message, _currentConversation);
            var fullResponse = new List<string>();

            await foreach (var chunk in streamResponse)
            {
                Console.Write(chunk);
                fullResponse.Add(chunk);
            }
            
            Console.WriteLine();
            
            // Add to conversation history
            _currentConversation.Messages.Add(new ChatMessage
            {
                Role = Role.User,
                Content = message
            });
            
            _currentConversation.Messages.Add(new ChatMessage
            {
                Role = Role.Assistant,
                Content = string.Join("", fullResponse)
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Streaming error: {ex.Message}");
        }
        
        Console.WriteLine();
    }
}
