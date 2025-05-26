using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MCPServerSDK.Services;

namespace MCPServerSDK;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        
        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        // Register MCP server services
        builder.Services.AddSingleton<IReportServer, ReportServerClient>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<ReportServerClient>();
            var reportServerAddress = builder.Configuration["ReportServer:Address"] ?? "http://localhost:1099/ReportServer";
            try
            {
                return new ReportServerClient(loggerFactory, reportServerAddress);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to initialize ReportServer client, falling back to stub implementation");
                throw new InvalidOperationException("ReportServer client initialization failed", ex);
            }
        });
        builder.Services.AddSingleton<McpReportServer>();
        builder.Services.AddHostedService<McpServerHostedService>();
        
        var host = builder.Build();
        
        try
        {
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Application terminated unexpectedly");
            throw;
        }
    }
}
