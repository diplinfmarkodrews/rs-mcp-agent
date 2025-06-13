using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.SemanticKernel;
using OpenAPISwaggerUI;
using ReportServerRPCClient.Extensions;
using RsMcpServer.Identity.Extensions;
using RsMcpServer.Identity.Services;
using RsMcpServer.Web.Extensions;
using RsMcpServer.Web.McpTools;


// Make the Program accessible to the test project
[assembly: InternalsVisibleTo("TestRsMcpServer")]

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddLogging(logging => 
{
    logging.AddConsole();
    logging.AddDebug();
});

// Add Keycloak authentication with enhanced features
builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddOpenApi();
var reportServerAddress = builder.Configuration["ReportServer:Address"] 
                          ?? throw new InvalidOperationException("ReportServer:Address");
                          
// Register the ReportServerRpcClient first
builder.Services.AddReportServerRpcClient(reportServerAddress); 

// Then register TerminalTool which depends on IReportServerClient
builder.Services.AddScoped<TerminalTool>();

// Create the kernel after all dependencies are registered
var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.Services.AddReportServerRpcClient(reportServerAddress)
    .AddLogging()
    .AddKeycloakAuthentication(builder.Configuration, builder.Environment)
    .AddSingleton<ISessionBridgeService, SessionBridgeService>()
    .AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
kernelBuilder.Plugins.AddFromType<TerminalTool>();
var kernel = kernelBuilder.Build();

// First set up service collection
builder.Services.AddSingleton(kernel);
var mcpBuilder = builder.Services.AddMcpServer()
    .WithTools(kernel.Plugins)
    // .WithHttpLogging(HttpLoggingFields.All, -1, -1)
    .WithHttpTransport();

// Build the app AFTER all configuration is done
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseOpenApi();
app.UseHttpsRedirection();
app.UseOpenAPISwaggerUI();

// This includes session, authentication, and authorization
app.UseKeycloakAuthentication(); 

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
