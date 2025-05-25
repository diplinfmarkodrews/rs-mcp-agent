using MCPServer.Models;

namespace MCPServer.Services;

public interface IReportServer : IDisposable
{
    Task<ReportResult> GenerateReportAsync(
        string templateId,
        Dictionary<string, string> parameters,
        ReportServer.OutputFormat outputFormat = ReportServer.OutputFormat.Pdf,
        bool includeCharts = true,
        CancellationToken cancellationToken = default);

    Task<ReportServer.ReportTemplatesResponse> GetAvailableReportTemplatesAsync(
        CancellationToken cancellationToken = default);

    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
}
