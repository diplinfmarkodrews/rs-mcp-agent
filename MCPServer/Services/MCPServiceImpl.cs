using Grpc.Core;
using MCPServer.Protos;
using MCPServer.Models;
using ChatMessage = MCPServer.Protos.ChatMessage;
using ChatResponse = MCPServer.Protos.ChatResponse;

namespace MCPServer.Services;

public class MCPServiceImpl : MCPService.MCPServiceBase
{
    private readonly ILogger<MCPServiceImpl> _logger;
    private readonly IReportServer _reportServerClient;

    public MCPServiceImpl(
        ILogger<MCPServiceImpl> logger,
        IReportServer reportServerClient)
    {
        _logger = logger;
        _reportServerClient = reportServerClient;
    }

    public override async Task<ChatResponse> Chat(ChatRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Received chat request");
        
        try
        {
            // All chat requests are treated as report-related queries in MCP Server
            return await HandleReportQuery(request, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            throw new RpcException(new Status(StatusCode.Internal, $"Error processing request: {ex.Message}"));
        }
    }

    public override async Task ChatStream(ChatRequest request, IServerStreamWriter<ChatResponseChunk> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("Received streaming chat request");
        
        try
        {
            // Handle the report query and stream back the response
            var response = await HandleReportQuery(request, context);
            
            // Stream the response in chunks
            var content = response.Message.Content;
            const int chunkSize = 100;
            
            for (int i = 0; i < content.Length; i += chunkSize)
            {
                var chunk = content.Substring(i, Math.Min(chunkSize, content.Length - i));
                await responseStream.WriteAsync(new ChatResponseChunk
                {
                    ContentChunk = chunk,
                    IsFinal = i + chunkSize >= content.Length,
                    Model = request.Model,
                    FinishReason = i + chunkSize >= content.Length ? "stop" : null
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing streaming chat request");
            throw new RpcException(new Status(StatusCode.Internal, $"Error processing request: {ex.Message}"));
        }
    }

    public override async Task<HealthCheckResponse> HealthCheck(HealthCheckRequest request, ServerCallContext context)
    {
        try
        {
            // Check ReportServer status
            var reportServerStatus = "unknown";
            try
            {
                reportServerStatus = await _reportServerClient.CheckHealthAsync(context.CancellationToken) ? "healthy" : "degraded";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking ReportServer health");
                reportServerStatus = "unhealthy";
            }

            var isHealthy = reportServerStatus == "healthy";
            return new HealthCheckResponse
            {
                Status = isHealthy ? HealthCheckResponse.Types.ServingStatus.Serving : HealthCheckResponse.Types.ServingStatus.NotServing
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking service health");
            return new HealthCheckResponse
            {
                Status = HealthCheckResponse.Types.ServingStatus.NotServing
            };
        }
    }
    
    // Handle report-related queries
    private async Task<ChatResponse> HandleReportQuery(ChatRequest request, ServerCallContext context)
    {
        var lastMessage = request.Messages.Last();
        string query = lastMessage.Content.ToLowerInvariant();
        
        try
        {
            // Check if the user is asking for available report templates
            if (query.Contains("available") || query.Contains("list") || query.Contains("what reports"))
            {
                var templates = await _reportServerClient.GetAvailableReportTemplatesAsync(context.CancellationToken);
                
                // Format the templates into a nice response
                var templatesList = templates.Templates
                    .Select(t => $"- {t.Name}: {t.Description}")
                    .ToList();
                
                string content = "Here are the available report templates:\n\n" + 
                    string.Join("\n", templatesList) + 
                    "\n\nYou can ask me to generate any of these reports.";
                
                return new ChatResponse
                {
                    Message = new ChatMessage
                    {
                        Role = Protos.Role.Assistant,
                        Content = content
                    },
                    Model = request.Model,
                    FinishReason = "stop"
                };
            }
            
            // Check if the user is asking to generate a specific report
            if (query.Contains("generate") || query.Contains("create") || query.Contains("run"))
            {
                // For demo purposes, let's use a fixed template and parameters
                // In a real implementation, you would parse the user's request to extract template and parameters
                string templateId = "monthly-summary";
                var parameters = new Dictionary<string, string>
                {
                    { "month", "May" },
                    { "year", "2025" }
                };
                
                var reportResponse = await _reportServerClient.GenerateReportAsync(
                    templateId, 
                    parameters, 
                    ReportServer.OutputFormat.Pdf, 
                    true, 
                    context.CancellationToken);
                
                if (reportResponse.Success)
                {
                    return new ChatResponse
                    {
                        Message = new ChatMessage
                        {
                            Role = Protos.Role.Assistant,
                            Content = $"I've generated the report for you. You can download it as '{reportResponse.ReportFilename}'."
                        },
                        Model = request.Model,
                        FinishReason = "stop",
                        Metadata = { { "reportId", Guid.NewGuid().ToString() } }
                    };
                }
                else
                {
                    return new ChatResponse
                    {
                        Message = new ChatMessage
                        {
                            Role = Protos.Role.Assistant,
                            Content = $"I couldn't generate the report. Error: {reportResponse.ErrorMessage}"
                        },
                        Model = request.Model,
                        FinishReason = "stop"
                    };
                }
            }
            
            // If we can't determine the specific report action, provide a helpful response
            return new ChatResponse
            {
                Message = new ChatMessage
                {
                    Role = Protos.Role.Assistant,
                    Content = "I can help you with reports. Would you like to see a list of available report templates, or generate a specific report?"
                },
                Model = request.Model,
                FinishReason = "stop"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling report query");
            throw new RpcException(new Status(StatusCode.Internal, $"Error processing report request: {ex.Message}"));
        }
    }
}
