using Grpc.Core;
using Grpc.Net.Client;
using MCPServer.Protos;
using MCPChatClient.Models;
using MCPChatClient.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MCPChatClient.Services;

/// <summary>
/// Service for communicating with the MCP Server via gRPC
/// </summary>
public class MCPClientService : IMCPClientService, IDisposable
{
    private readonly ILogger<MCPClientService> _logger;
    private readonly MCPServerSettings _settings;
    private readonly GrpcChannel _channel;
    private readonly MCPService.MCPServiceClient _client;

    public MCPClientService(
        ILogger<MCPClientService> logger,
        IOptions<MCPServerSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        
        _channel = GrpcChannel.ForAddress(_settings.Address);
        _client = new MCPService.MCPServiceClient(_channel);
        
        _logger.LogInformation("MCP Client Service initialized for server: {Address}", _settings.Address);
    }

    public async Task<ChatServiceResponse> SendChatRequestAsync(ChatRequest request)
    {
        try
        {
            _logger.LogDebug("Sending chat request to MCP server");
            
            var response = await _client.ChatAsync(request);
            
            return new ChatServiceResponse
            {
                Content = response.Message.Content,
                Model = response.Model,
                FinishReason = response.FinishReason,
                Metadata = response.Metadata.ToDictionary(kv => kv.Key, kv => kv.Value),
                IsFromMCP = true
            };
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error occurred while sending chat request");
            throw new InvalidOperationException($"MCP Server communication failed: {ex.Status.Detail}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while sending chat request");
            throw;
        }
    }

    public async Task<IAsyncEnumerable<string>> SendStreamingChatRequestAsync(ChatRequest request)
    {
        try
        {
            _logger.LogDebug("Sending streaming chat request to MCP server");
            
            var call = _client.ChatStream(request);
            return ProcessStreamingResponse(call.ResponseStream);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error occurred while sending streaming chat request");
            throw new InvalidOperationException($"MCP Server streaming communication failed: {ex.Status.Detail}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while sending streaming chat request");
            throw;
        }
    }

    private async IAsyncEnumerable<string> ProcessStreamingResponse(IAsyncStreamReader<ChatResponseChunk> responseStream)
    {
        await foreach (var chunk in responseStream.ReadAllAsync())
        {
            if (!string.IsNullOrEmpty(chunk.ContentChunk))
            {
                yield return chunk.ContentChunk;
            }
            
            if (chunk.IsFinal)
            {
                break;
            }
        }
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var healthRequest = new HealthCheckRequest { Service = "mcp" };
            var response = await _client.HealthCheckAsync(healthRequest);
            
            var isHealthy = response.Status == HealthCheckResponse.Types.ServingStatus.Serving;
            _logger.LogDebug("MCP Server health check result: {Status}", response.Status);
            
            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for MCP server");
            return false;
        }
    }

    public async Task<MCPServiceResponse> GetServerStatusAsync()
    {
        try
        {
            var isHealthy = await CheckHealthAsync();
            return new MCPServiceResponse
            {
                IsHealthy = isHealthy,
                StatusMessage = isHealthy ? "Server is healthy" : "Server is not responding"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get server status");
            return new MCPServiceResponse
            {
                IsHealthy = false,
                StatusMessage = $"Status check failed: {ex.Message}"
            };
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        GC.SuppressFinalize(this);
    }
}
