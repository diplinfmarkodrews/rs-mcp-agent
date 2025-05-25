using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MCPServer.Models;
using MCPServer.Protos;

namespace MCPServer.Services
{
    /// <summary>
    /// Stub implementation of ReportServerClient for testing when JNI is not available
    /// </summary>
    public class StubReportServerClient : IReportServer
    {
        private readonly ILogger<StubReportServerClient> _logger;
        private readonly string _serverAddress;

        public StubReportServerClient(ILogger<StubReportServerClient> logger, string serverAddress)
        {
            _logger = logger;
            _serverAddress = serverAddress;
            _logger.LogInformation($"Initialized stub ReportServer client connecting to {serverAddress}");
        }

        public async Task<ReportResult> GenerateReportAsync(
            string templateId,
            Dictionary<string, string> parameters,
            ReportServer.OutputFormat outputFormat = ReportServer.OutputFormat.Pdf,
            bool includeCharts = true,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken); // Simulate network delay
            
            _logger.LogInformation($"Stub: Generating report for template {templateId}");
            return new ReportResult
            {
                Success = true,
                ReportData = Array.Empty<byte>(),
                ReportMimeType = "application/pdf",
                ReportFilename = $"{templateId}_stub.pdf"
            };
        }

        public async Task<ReportServer.ReportTemplatesResponse> GetAvailableReportTemplatesAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken); // Simulate network delay
            
            _logger.LogInformation("Stub: Retrieving available report templates");
            var response = new ReportServer.ReportTemplatesResponse();
            
            // Add some stub templates
            response.Templates.Add(new ReportServer.ReportTemplate
            {
                Id = "monthly-summary",
                Name = "Monthly Summary Report",
                Description = "A summary of monthly activities (Stub)",
                RequiredParameters = 
                {
                    new ReportServer.ParameterDefinition
                    {
                        Name = "month",
                        Description = "Month (1-12)",
                        Type = ReportServer.ParameterType.Number,
                        Required = true
                    },
                    new ReportServer.ParameterDefinition
                    {
                        Name = "year",
                        Description = "Year (YYYY)",
                        Type = ReportServer.ParameterType.Number,
                        Required = true
                    }
                },
                SupportedFormats = { ReportServer.OutputFormat.Pdf, ReportServer.OutputFormat.Html }
            });
            
            return response;
        }

        public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken); // Simulate network delay
            
            _logger.LogInformation("Stub: Checking health");
            return true;
        }

        public void Dispose()
        {
            // No resources to dispose in stub implementation
            GC.SuppressFinalize(this);
        }
    }


}
