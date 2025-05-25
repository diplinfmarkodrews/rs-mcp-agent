using MCPChatClient.Models;
using MCPChatClient.Services;
using Microsoft.AspNetCore.Mvc;

namespace MCPChatClient.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly IChatService _chatService;
    private readonly IMCPClientService _mcpClient;

    public ChatController(
        ILogger<ChatController> logger,
        IChatService chatService,
        IMCPClientService mcpClient)
    {
        _logger = logger;
        _chatService = chatService;
        _mcpClient = mcpClient;
    }

    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message cannot be empty");
            }

            var conversation = request.ConversationId != null 
                ? new ChatConversation { ConversationId = request.ConversationId }
                : null;

            var response = await _chatService.SendMessageAsync(request.Message, conversation);
            
            return Ok(new
            {
                content = response.Content,
                model = response.Model,
                finishReason = response.FinishReason,
                metadata = response.Metadata,
                isFromMCP = response.IsFromMCP,
                timestamp = response.Timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("stream/{message}")]
    public async Task StreamMessage(string message)
    {
        try
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            var streamResponse = await _chatService.SendMessageStreamAsync(message);
            
            await foreach (var chunk in streamResponse)
            {
                await Response.WriteAsync($"data: {chunk}\n\n");
                await Response.Body.FlushAsync();
            }
            
            await Response.WriteAsync("data: [DONE]\n\n");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing streaming chat message");
            await Response.WriteAsync($"data: Error: {ex.Message}\n\n");
        }
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var status = await _mcpClient.GetServerStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? ConversationId { get; set; }
    }
}
