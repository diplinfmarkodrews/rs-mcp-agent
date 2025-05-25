using MCPChatClient.Models;
using MCPChatClient.Services;
using MCPChatClient.UI;
using MCPChatClient.Web.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MCPChatClient;

/// <summary>
/// Main entry point for the MCP Chat Client application
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            // Check if we should run in web mode
            bool runWebMode = args.Contains("--web") || args.Contains("-w");
            
            if (runWebMode)
            {
                await RunWebMode(args);
            }
            else
            {
                await RunConsoleMode(args);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Application failed to start: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    private static async Task RunConsoleMode(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        using (var scope = host.Services.CreateScope())
        {
            var consoleUI = scope.ServiceProvider.GetRequiredService<ConsoleUI>();
            await consoleUI.RunAsync();
        }
    }

    private static async Task RunWebMode(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Add services to the container
        ConfigureServices(builder.Services, builder.Configuration);
        
        // Add controllers for web API
        builder.Services.AddControllers();
        
        var app = builder.Build();
        
        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        
        app.UseStaticFiles();
        app.UseRouting();
        app.MapControllers();
        
        // Serve the web UI on the root
        app.MapFallbackToFile("index.html");
        
        Console.WriteLine("🌐 Starting MCP Chat Client Web Server...");
        Console.WriteLine("📱 Web UI available at: http://localhost:5001");
        Console.WriteLine("🔌 API available at: http://localhost:5001/api/chat");
        
        await app.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services, context.Configuration);
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                
                // Set log levels based on environment
                if (context.HostingEnvironment.IsDevelopment())
                {
                    logging.SetMinimumLevel(LogLevel.Debug);
                }
                else
                {
                    logging.SetMinimumLevel(LogLevel.Information);
                }
            });

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<ChatAISettings>(
            configuration.GetSection("ChatAI"));
        services.Configure<MCPServerSettings>(
            configuration.GetSection("MCPServer"));

        // Services
        services.AddSingleton<IMCPClientService, MCPClientService>();
        services.AddSingleton<IChatService, ChatAIService>();
        services.AddTransient<ConsoleUI>();

        // HTTP Client for potential future use
        services.AddHttpClient();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }
}
