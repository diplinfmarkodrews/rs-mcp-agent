using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;
using System.Linq;
using ReportServerPort;
using static MCPServerSDK.Models.ReportServer;

namespace MCPServerSDK.Services;

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
    // [Description("Gets a list of all available report templates")]
    // public async Task<GetTemplatesResult> GetReportTemplatesAsync(CancellationToken cancellationToken = default)
    // {
    //     _logger.LogInformation("Getting available report templates");        
    //     var response = await _reportServer. GetAvailableReportTemplatesAsync(cancellationToken);
    //     
    //     // Convert the ReportServer.ReportTemplate to the McpReportServer.ReportTemplate format
    //     var templates = response.Templates.Select(t => new ReportTemplate
    //     {
    //         Id = t.Id,
    //         Name = t.Name,
    //         Description = t.Description,
    //         RequiredParameters = t.RequiredParameters.Select(p => new ParameterDefinition
    //         {
    //             Name = p.Name,
    //             Description = p.Description,
    //             Type = p.Type.ToString(),
    //             Required = p.Required
    //         }).ToList(),
    //         SupportedFormats = t.SupportedFormats
    //     }).ToList();
    //     
    //     return new GetTemplatesResult { Templates = templates };
    // }

    /// <summary>
    /// Generates a report based on template and parameters
    /// </summary>
    // [Description("Generates a report using the specified template and parameters")]
    // public async Task<GenerateReportResult> GenerateReportAsync(
    //     [Description("The ID of the report template to use")] string templateId,
    //     [Description("Parameters for the report generation")] Dictionary<string, object> parameters,
    //     [Description("Output format (pdf, html, excel)")] string format = "pdf",
    //     [Description("Whether to include charts and visualizations")] bool includeCharts = true)
    // {
    //     _logger.LogInformation("Generating report: Template={TemplateId}, Format={Format}, Parameters={@Parameters}", 
    //         templateId, format, parameters);
    //
    //     // Validate template exists
    //     var templatesResult = await GetReportTemplatesAsync();
    //     var template = templatesResult.Templates.FirstOrDefault(t => t.Id == templateId);
    //     if (template == null)
    //     {
    //         return new GenerateReportResult
    //         {
    //             Success = false,
    //             ErrorMessage = $"Template '{templateId}' not found"
    //         };
    //     }
    //
    //     // Validate required parameters
    //     var missingParams = template.RequiredParameters
    //         .Where(p => p.Required && !parameters.ContainsKey(p.Name))
    //         .ToList();
    //
    //     if (missingParams.Any())
    //     {
    //         return new GenerateReportResult
    //         {
    //             Success = false,
    //             ErrorMessage = $"Missing required parameters: {string.Join(", ", missingParams.Select(p => p.Name))}"
    //         };
    //     }
    //
    //     // Validate format
    //     if (!template.SupportedFormats.Contains(format.ToLower()))
    //     {
    //         return new GenerateReportResult
    //         {
    //             Success = false,
    //             ErrorMessage = $"Format '{format}' not supported for template '{templateId}'. Supported formats: {string.Join(", ", template.SupportedFormats)}"
    //         };
    //     }
    //
    //     // Convert parameters from object to string
    //     var stringParameters = parameters.ToDictionary(
    //         kvp => kvp.Key, 
    //         kvp => kvp.Value?.ToString() ?? string.Empty
    //     );
    //     
    //     // Convert format string to OutputFormat enum
    //     if (!Enum.TryParse<OutputFormat>(format, true, out var outputFormat))
    //     {
    //         outputFormat = OutputFormat.Pdf; // Default to PDF
    //     }
    //
    //     var reportResult = await _reportServer.GenerateReportAsync(templateId, stringParameters, outputFormat, includeCharts);
    //     
    //     if (!reportResult.Success)
    //     {
    //         return new GenerateReportResult
    //         {
    //             Success = false,
    //             ErrorMessage = reportResult.ErrorMessage
    //         };
    //     }
    //     
    //     var mimeType = GetMimeType(format);
    //     var filename = $"{templateId}_{DateTime.Now:yyyyMMdd_HHmmss}.{format.ToLower()}";
    //
    //     return new GenerateReportResult
    //     {
    //         Success = true,
    //         ReportData = reportResult.ReportData,
    //         MimeType = mimeType,
    //         Filename = filename,
    //         Size = reportResult.ReportData?.Length ?? 0,
    //         GeneratedAt = DateTime.UtcNow
    //     };
    // }

    /// <summary>
    /// Gets the health status of the report server
    /// </summary>
    // [Description("Checks the health status of the report generation service")]
    // public async Task<HealthStatus> GetHealthStatusAsync()
    // {
    //     _logger.LogInformation("Checking health status");
    //     
    //     var health = await _reportServer.CheckHealthAsync();
    //     
    //     return new HealthStatus
    //     {
    //         IsHealthy = true,
    //         Status = "Healthy",
    //         Timestamp = DateTime.UtcNow,
    //         Version = "1.0.0",
    //         Details = new Dictionary<string, object>
    //         {
    //             ["ReportEngine"] = health,
    //             ["Database"] = "Connected",
    //             ["TemplateCache"] = "Loaded",
    //             ["QueueLength"] = 0
    //         }
    //     };
    // }

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
