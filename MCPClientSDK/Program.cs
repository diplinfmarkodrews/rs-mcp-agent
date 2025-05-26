using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MCPClientSDK.Services;

namespace MCPClientSDK;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        
        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        
        // Register services
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<McpClientService>();
        builder.Services.AddSingleton<InteractiveClient>();
        
        var host = builder.Build();
        
        try
        {
            var client = host.Services.GetRequiredService<InteractiveClient>();
            await client.RunAsync();
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Application terminated unexpectedly");
            throw;
        }
    }
}
