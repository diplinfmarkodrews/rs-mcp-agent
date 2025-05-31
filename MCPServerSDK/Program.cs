using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MCPServerSDK.Services;
// using MCPServerSDK.Controller;
using ReportServerRPCClient.Extensions;

namespace MCPServerSDK;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        // Register MCP server services
        var reportServerAddress = builder.Configuration["ReportServer:Address"] ?? "http://localhost:1099/";
        builder.Services.AddReportServerRpcClient(reportServerAddress); 
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
