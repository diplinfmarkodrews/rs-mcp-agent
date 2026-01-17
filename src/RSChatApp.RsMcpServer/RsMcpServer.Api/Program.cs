using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.SemanticKernel;
using OpenAPISwaggerUI;
using ReportServer.RpcClient.Extensions;
using RSChatApp.Mcp.ReportServer.Tools;
using RsMcpServer.Identity.Extensions;
using RsMcpServer.Identity.Middleware;
using RsMcpServer.Web.Extensions;
using Serilog;


// Make the Program accessible to the test project
[assembly: InternalsVisibleTo("TestRsMcpServer")]

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddSerilog(new LoggerConfiguration()
        .WriteTo.File("Logs/rsmcpserver-.log", rollingInterval: RollingInterval.Day)
        .CreateLogger());
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});
builder.Services.AddHealthChecks();
var reportServerUrl = builder.Configuration["ReportServer:Url"] 
                      ?? throw new InvalidOperationException("ReportServer:Url");

builder.Services.AddReportServerRpcClient(reportServerUrl);
// Add Keycloak authentication with enhanced features
builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment, setupSessionBridge: true);
builder.Services.AddLegacyAuthentication();

builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();


// Create kernel and register plugins
builder.Services.AddSingleton((serviceProvider) => {
    KernelPluginCollection pluginCollection = [];
    pluginCollection.AddFromType<TerminalTool>("TerminalTool", serviceProvider);
    return pluginCollection;
});

// Register IEnumerable<KernelPlugin> for the Kernel constructor
builder.Services.AddSingleton<IEnumerable<KernelPlugin>>((serviceProvider) => {
    var pluginCollection = serviceProvider.GetRequiredService<KernelPluginCollection>();
    return pluginCollection;
});

// Create the kernel service
builder.Services.AddSingleton<Kernel>((serviceProvider) => {
    KernelPluginCollection pluginCollection = serviceProvider.GetRequiredService<KernelPluginCollection>();
    return new Kernel(serviceProvider, pluginCollection);
});

builder.Services.AddMcpServer()
    .WithTools<TerminalTool>()
    // .WithHttpLogging(HttpLoggingFields.All, -1, -1)
    .WithHttpTransport();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseOpenApi();
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseOpenAPISwaggerUI();
// Map health check endpoints
app.MapHealthChecks("/health");

// This includes session, authentication, and authorization
app.UseKeycloakAuthentication()
   .UseAuthenticatedSession();
    
app.MapAuthenticationEndpoints();
app.MapRsRestEndpoints();
// Map MCP endpoints
app.MapMcp()
    .WithHttpLogging(HttpLoggingFields.All)
    .WithDescription("MCP Server for the Report Server")
    .WithOpenApi()
    
    // .RequireAuthorization()
    ; // Require authentication for MCP endpoints

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
// for testing purposes
namespace RSChatApp.RsMcpServer.Web
{
    public partial class Program { }
}

