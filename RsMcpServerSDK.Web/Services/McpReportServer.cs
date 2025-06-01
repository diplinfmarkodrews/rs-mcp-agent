using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;
using System.Linq;
using ModelContextProtocol.Server;
using ReportServerPort;

namespace RsMCPServerSDK.Web.Services;

/// <summary>
/// MCP Server implementation for report generation using Microsoft.Extensions.AI MCP SDK
/// </summary>
public class McpReportServer
{
    private readonly ILogger<McpReportServer> _logger;
    private readonly IReportServerClient _reportServer;

    public McpReportServer(ILogger<McpReportServer> logger, IReportServerClient reportServer)
    {
        _logger = logger;
        _reportServer = reportServer;
    }

    /// <summary>
    /// Gets available report templates
    /// </summary>
    [McpServerTool, Description("Gets a list of all available report templates")]
    public async Task<GetTemplatesResult> GetReportTemplatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting available report templates");        
        
        
        return new GetTemplatesResult { Templates = [new ReportTemplate{Description = "sdjfhasjkdf", Id = "123", Name = "sfdkajsd"}] }; 
    }
    
    /// <summary>
    /// Gets the health status of the report server
    /// </summary>
    [Description("Checks the health status of the report generation service")]
    public async Task<HealthStatus> GetHealthStatusAsync()
    {
        _logger.LogInformation("Checking health status");
        
        // var health = await _reportServer.CheckHealthAsync();
        
        return new HealthStatus
        {
            IsHealthy = true,
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Details = new Dictionary<string, object>
            {
                ["ReportEngine"] = "degraded",
                ["Database"] = "Connected",
                ["TemplateCache"] = "Loaded",
                ["QueueLength"] = 0
            }
        };
    }

    private static byte[] GenerateSampleReportData(string templateId, Dictionary<string, object> parameters, string format, bool includeCharts)
    {
        var report = new
        {
            Template = templateId,
            Parameters = parameters,
            Format = format,
            IncludeCharts = includeCharts,
            GeneratedAt = DateTime.UtcNow,
            Content = $"This is a sample {templateId} report generated in {format} format."
        };

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    private static string GetMimeType(string format) => format.ToLower() switch
    {
        "pdf" => "application/pdf",
        "html" => "text/html",
        "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        _ => "application/octet-stream"
    };
}

// Data models for MCP responses
public class GetTemplatesResult
{
    public List<ReportTemplate> Templates { get; set; } = new();
}

public class ReportTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ParameterDefinition> RequiredParameters { get; set; } = new();
    public List<string> SupportedFormats { get; set; } = new();
}

public class ParameterDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Required { get; set; }
}

public class GenerateReportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public byte[]? ReportData { get; set; }
    public string? MimeType { get; set; }
    public string? Filename { get; set; }
    public int Size { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class HealthStatus
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
}
