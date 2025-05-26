using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MCPClientSDK.Services;

/// <summary>
/// MCP Client service for connecting to the report server
/// </summary>
public class McpClientService
{
    private readonly ILogger<McpClientService> _logger;
    private readonly HttpClient _httpClient;

    public McpClientService(ILogger<McpClientService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Simulates MCP function call to get report templates
    /// </summary>
    public async Task<GetTemplatesResult?> GetReportTemplatesAsync()
    {
        _logger.LogInformation("Calling MCP function: GetReportTemplatesAsync");

        try
        {
            // Simulate MCP function call (in real implementation, this would use the MCP protocol)
            await Task.Delay(100);

            var templates = new List<ReportTemplate>
            {
                new()
                {
                    Id = "monthly-summary",
                    Name = "Monthly Summary Report",
                    Description = "A comprehensive summary of monthly activities and metrics",
                    RequiredParameters = new()
                    {
                        new() { Name = "month", Description = "Month (1-12)", Type = "number", Required = true },
                        new() { Name = "year", Description = "Year (YYYY)", Type = "number", Required = true },
                        new() { Name = "department", Description = "Department name", Type = "string", Required = false }
                    },
                    SupportedFormats = new() { "pdf", "html", "excel" }
                },
                new()
                {
                    Id = "quarterly-performance",
                    Name = "Quarterly Performance Report",
                    Description = "Detailed quarterly performance analysis",
                    RequiredParameters = new()
                    {
                        new() { Name = "quarter", Description = "Quarter (1-4)", Type = "number", Required = true },
                        new() { Name = "year", Description = "Year (YYYY)", Type = "number", Required = true },
                        new() { Name = "includeCharts", Description = "Include visual charts", Type = "boolean", Required = false }
                    },
                    SupportedFormats = new() { "pdf", "html" }
                },
                new()
                {
                    Id = "annual-budget",
                    Name = "Annual Budget Report",
                    Description = "Complete annual budget analysis and forecasting",
                    RequiredParameters = new()
                    {
                        new() { Name = "year", Description = "Year (YYYY)", Type = "number", Required = true },
                        new() { Name = "costCenter", Description = "Cost center code", Type = "string", Required = false }
                    },
                    SupportedFormats = new() { "pdf", "excel" }
                }
            };

            return new GetTemplatesResult { Templates = templates };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get report templates");
            return null;
        }
    }

    /// <summary>
    /// Simulates MCP function call to generate a report
    /// </summary>
    public async Task<GenerateReportResult?> GenerateReportAsync(
        string templateId,
        Dictionary<string, object> parameters,
        string format = "pdf",
        bool includeCharts = true)
    {
        _logger.LogInformation("Calling MCP function: GenerateReportAsync with template {TemplateId}", templateId);

        try
        {
            // Validate template exists
            var templatesResult = await GetReportTemplatesAsync();
            var template = templatesResult?.Templates.FirstOrDefault(t => t.Id == templateId);
            if (template == null)
            {
                return new GenerateReportResult
                {
                    Success = false,
                    ErrorMessage = $"Template '{templateId}' not found"
                };
            }

            // Validate required parameters
            var missingParams = template.RequiredParameters
                .Where(p => p.Required && !parameters.ContainsKey(p.Name))
                .ToList();

            if (missingParams.Any())
            {
                return new GenerateReportResult
                {
                    Success = false,
                    ErrorMessage = $"Missing required parameters: {string.Join(", ", missingParams.Select(p => p.Name))}"
                };
            }

            // Validate format
            if (!template.SupportedFormats.Contains(format.ToLower()))
            {
                return new GenerateReportResult
                {
                    Success = false,
                    ErrorMessage = $"Format '{format}' not supported for template '{templateId}'. Supported formats: {string.Join(", ", template.SupportedFormats)}"
                };
            }

            // Simulate report generation (with progress)
            _logger.LogInformation("Generating report...");
            await Task.Delay(1000);
            _logger.LogInformation("Report generation 50% complete...");
            await Task.Delay(1000);
            _logger.LogInformation("Report generation complete!");

            var reportData = GenerateSampleReportData(templateId, parameters, format, includeCharts);
            var mimeType = GetMimeType(format);
            var filename = $"{templateId}_{DateTime.Now:yyyyMMdd_HHmmss}.{format.ToLower()}";

            return new GenerateReportResult
            {
                Success = true,
                ReportData = reportData,
                MimeType = mimeType,
                Filename = filename,
                Size = reportData.Length,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate report");
            return new GenerateReportResult
            {
                Success = false,
                ErrorMessage = $"Report generation failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Simulates MCP function call to get health status
    /// </summary>
    public async Task<HealthStatus?> GetHealthStatusAsync()
    {
        _logger.LogInformation("Calling MCP function: GetHealthStatusAsync");

        try
        {
            await Task.Delay(50);

            return new HealthStatus
            {
                IsHealthy = true,
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Details = new Dictionary<string, object>
                {
                    ["ReportEngine"] = "Available",
                    ["Database"] = "Connected",
                    ["TemplateCache"] = "Loaded",
                    ["QueueLength"] = 0
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health status");
            return null;
        }
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
            Content = $"This is a sample {templateId} report generated in {format} format.",
            Metadata = new
            {
                GeneratedBy = "MCP Report Server SDK",
                ClientVersion = "1.0.0",
                ProcessingTime = "2.1s"
            }
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

// Shared data models
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
