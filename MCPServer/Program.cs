using MCPServer.Services;
using MCPServer.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MCPServer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddGrpc();
        builder.Services.AddGrpcReflection();
        builder.Services.AddAuthorization();        // Configure ReportServer connection
        var reportServerAddress = builder.Configuration["ReportServer:Address"] ?? "http://localhost:8080";
        builder.Services.AddSingleton<IReportServer>(sp => 
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<ReportServerClient>();
            try
            {
                return new ReportServerClient(loggerFactory, reportServerAddress);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to initialize JNI ReportServer client, falling back to stub implementation");
                var stubLogger = loggerFactory.CreateLogger<StubReportServerClient>();
                return new StubReportServerClient(stubLogger, reportServerAddress);
            }
        });

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapGrpcReflectionService();
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();

        // Map the gRPC service
        app.MapGrpcService<MCPServiceImpl>();

        // Default endpoint
        app.MapGet("/", () => "Model Context Protocol (MCP) Server. Communication is handled through gRPC.");

        app.Run();
    }
}
