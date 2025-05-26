using Grpc.Core;
using MCPServer.Protos;
using MCPServer.Models;
using MCPServer.Attributes;
using System.ComponentModel;
using ChatMessage = MCPServer.Protos.ChatMessage;
using ChatResponse = MCPServer.Protos.ChatResponse;

namespace MCPServer.Services;

[Description("MCP (Model Context Protocol) service implementation providing chat-based report generation and AI interactions via gRPC")]
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

    /// <summary>
    /// Handles chat requests for report generation and AI interactions
    /// </summary>
    /// <param name="request">The chat request containing user message and parameters</param>
    /// <param name="context">The server call context for gRPC</param>
    /// <returns>A chat response with AI-generated content or report information</returns>
    [Description("Processes natural language chat requests for report generation and analysis. Supports queries for report templates, generation requests, and data analysis.")]
    [McpMethod("Processes natural language chat requests for report generation and analysis",
        Parameters = new[] { "request: Chat request with user messages", "context: gRPC server call context" },
        ReturnDescription = "Chat response with report information or AI-generated content",
        Examples = new[] { "Generate a monthly sales report", "Show me available report templates", "Create a quarterly performance analysis" })]
    public override async Task<ChatResponse> Chat(
        [Description("Chat request containing user messages and optional parameters")]
        [McpParameter("Chat request containing user messages and parameters", Required = true)] ChatRequest request, 
        ServerCallContext context)
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

    /// <summary>
    /// Handles streaming chat requests for real-time report generation and AI interactions
    /// </summary>
    /// <param name="request">The chat request containing user message and parameters</param>
    /// <param name="responseStream">The stream writer for sending chunked responses</param>
    /// <param name="context">The server call context for gRPC</param>
    /// <returns>A task representing the streaming operation</returns>
    [Description("Processes streaming chat requests for real-time report generation with chunked responses. Ideal for large reports or live data analysis.")]
    [McpMethod("Processes streaming chat requests for real-time report generation with chunked responses",
        Parameters = new[] { "request: Chat request with user messages", "responseStream: Stream for chunked responses", "context: gRPC server call context" },
        ReturnDescription = "Streaming task that sends chunked responses",
        Examples = new[] { "Generate large report with progress updates", "Stream live data analysis results" })]
    public override async Task ChatStream(
        [Description("Chat request containing user messages and optional parameters")]
        [McpParameter("Chat request containing user messages and parameters", Required = true)] ChatRequest request, 
        [Description("Response stream for sending chunked data to the client")]
        [McpParameter("Response stream for sending chunked data", Required = true)] IServerStreamWriter<ChatResponseChunk> responseStream, 
        ServerCallContext context)
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

    /// <summary>
    /// Performs a health check on the MCP service and its dependencies
    /// </summary>
    /// <param name="request">The health check request with optional service name</param>
    /// <param name="context">The server call context for gRPC</param>
    /// <returns>A health check response indicating service status</returns>
    [Description("Performs a comprehensive health check on the MCP service and all its dependencies including the report server backend.")]
    [McpMethod("Checks the health status of the MCP service and its dependencies",
        Parameters = new[] { "request: Health check request with optional service name", "context: gRPC server call context" },
        ReturnDescription = "Health check response indicating overall service status",
        Examples = new[] { "Check if MCP service is running", "Verify report server connectivity", "Monitor service health" })]
    public override async Task<HealthCheckResponse> HealthCheck(
        [Description("Health check request with optional service name to check")]
        [McpParameter("Health check request with optional service name", Required = true)] HealthCheckRequest request, 
        ServerCallContext context)
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
    
    /// <summary>
    /// Handles report-related queries by parsing user intent and routing to appropriate report operations
    /// </summary>
    /// <param name="request">The chat request containing user messages</param>
    /// <param name="context">The server call context for gRPC</param>
    /// <returns>A chat response with report information or instructions</returns>
    [Description("Internal method that handles report-related queries by parsing user intent and routing to appropriate report operations")]
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
