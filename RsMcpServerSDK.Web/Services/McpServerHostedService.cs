namespace RsMCPServerSDK.Web.Services;

/// <summary>
/// Hosted service that runs the MCP server
/// </summary>
public class McpServerHostedService : BackgroundService
{
    private readonly ILogger<McpServerHostedService> _logger;
    private readonly McpReportServer _reportServer;

    public McpServerHostedService(ILogger<McpServerHostedService> logger, McpReportServer reportServer)
    {
        _logger = logger;
        _reportServer = reportServer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MCP Report Server starting...");

        try
        {
            // Initialize the MCP server
            await InitializeServerAsync(stoppingToken);

            // Keep the server running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
                
                // Periodic health check
                try
                {
                    var health = await _reportServer.GetHealthStatusAsync();
                    _logger.LogDebug("Health check: {Status}", health.IsHealthy ? "Healthy" : "Unhealthy");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Health check failed");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MCP Report Server stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP Report Server encountered an error");
            throw;
        }
    }

    private async Task InitializeServerAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing MCP Report Server...");

        // Test the report server
        var health = await _reportServer.GetHealthStatusAsync();
        if (!health.IsHealthy)
        {
            throw new InvalidOperationException("Report server is not healthy");
        }

        //var templates = await _reportServer.GetReportTemplatesAsync();
        // _logger.LogInformation("Loaded {TemplateCount} report templates", templates.Templates.Count);

        _logger.LogInformation("MCP Report Server initialized successfully");
        _logger.LogInformation("Server is ready to handle requests");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MCP Report Server stopping...");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("MCP Report Server stopped");
    }
}
