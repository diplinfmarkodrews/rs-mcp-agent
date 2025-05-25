using MCPServer.Models;
using MCPServer.Services.Implementation;

namespace MCPServer.Services
{
    /// <summary>
    /// Client for communicating with the Java-based ReportServer via Java RMI with JNI bridge
    /// </summary>
    public class ReportServerClient : IReportServer
    {
        private readonly ILogger<ReportServerClient> _logger;
        private readonly string _serverAddress;
        private readonly JniReportServerImplementation? _jniImplementation;

        public ReportServerClient(ILoggerFactory loggerFactory, string serverAddress, bool isStubMode = false)
        {
            _logger = loggerFactory.CreateLogger<ReportServerClient>();
            _serverAddress = serverAddress;
            
            if (!isStubMode)
            {
                try
                {
                    _jniImplementation = new JniReportServerImplementation(loggerFactory, serverAddress);
                    _logger.LogInformation($"Initialized ReportServer JNI client connecting to {serverAddress}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error initializing Java Virtual Machine");
                    throw;
                }
            }
        }

        /// <summary>
        /// Generates a report using the ReportServer
        /// </summary>
        /// <param name="templateId">The template ID for the report</param>
        /// <param name="parameters">The parameters for the report</param>
        /// <param name="outputFormat">The desired output format</param>
        /// <param name="includeCharts">Whether to include charts in the report</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Report generation result</returns>
        public virtual async Task<ReportResult> GenerateReportAsync(
            string templateId, 
            Dictionary<string, string> parameters, 
            ReportServer.OutputFormat outputFormat = ReportServer.OutputFormat.Pdf, 
            bool includeCharts = true, 
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => {
                try
                {
                    if (_jniImplementation == null)
                    {
                        throw new InvalidOperationException("JNI implementation not initialized");
                    }

                    _logger.LogInformation($"Sending report generation request to ReportServer for template {templateId}");
                    return _jniImplementation.GenerateReport(
                        templateId,
                        parameters,
                        outputFormat.ToString().ToUpperInvariant(),
                        includeCharts);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error calling ReportServer to generate report with template {templateId}");
                    return new ReportResult
                    {
                        Success = false,
                        ErrorMessage = $"Error generating report: {ex.Message}",
                        ReportData = Array.Empty<byte>(),
                        ReportMimeType = "application/octet-stream",
                        ReportFilename = $"{templateId}_error.txt"
                    };
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Gets a list of available report templates from the ReportServer
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available report templates</returns>
        public virtual async Task<ReportServer.ReportTemplatesResponse> GetAvailableReportTemplatesAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => {
                try
                {
                    if (_jniImplementation == null)
                    {
                        throw new InvalidOperationException("JNI implementation not initialized");
                    }

                    _logger.LogInformation("Retrieving available report templates from ReportServer");
                    var templates = _jniImplementation.GetAvailableReportTemplates();
                    
                    var response = new ReportServer.ReportTemplatesResponse();
                    foreach (var template in templates)
                    {
                        var protoTemplate = new ReportServer.ReportTemplate
                        {
                            Id = template.Id,
                            Name = template.Name,
                            Description = template.Description
                        };
                        
                        foreach (var param in template.RequiredParameters)
                        {
                            var paramType = Enum.TryParse<ReportServer.ParameterType>(param.Type, true, out var type)
                                ? type
                                : ReportServer.ParameterType.String;
                                
                            protoTemplate.RequiredParameters.Add(new ReportServer.ParameterDefinition
                            {
                                Name = param.Name,
                                Description = param.Description,
                                Type = paramType,
                                Required = param.Required,
                                DefaultValue = param.DefaultValue
                            });
                        }
                        
                        foreach (var format in template.SupportedFormats)
                        {
                            if (Enum.TryParse<ReportServer.OutputFormat>(format, true, out var outputFormat))
                            {
                                protoTemplate.SupportedFormats.Add(outputFormat);
                            }
                        }
                        
                        response.Templates.Add(protoTemplate);
                    }
                    
                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving report templates from ReportServer");
                    return new ReportServer.ReportTemplatesResponse();
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Checks the health of the ReportServer
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the server is healthy, false otherwise</returns>
        public virtual async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => {
                try
                {
                    if (_jniImplementation == null)
                    {
                        throw new InvalidOperationException("JNI implementation not initialized");
                    }

                    _logger.LogInformation("Checking ReportServer health");
                    return _jniImplementation.CheckHealth();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking ReportServer health");
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Closes the JNI resources
        /// </summary>
        public virtual void Dispose()
        {
            _jniImplementation?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
