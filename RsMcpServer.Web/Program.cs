using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using OpenAPISwaggerUI;
using ReportServerPort;
using ReportServerRPCClient.Extensions;
using RsMcpServer.Identity.Extensions;
using RsMcpServer.Identity.Middleware;
using RsMcpServer.Identity.Services;
using RsMcpServer.Web.Extensions;
using RsMcpServer.Web.Mcp.Tools;

// Make the Program accessible to the test project
[assembly: InternalsVisibleTo("TestRsMcpServer")]

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(logging => 
{
    logging.AddConsole();
    logging.AddDebug();
});
builder.Services.AddHealthChecks();
// Add Keycloak authentication with enhanced features
builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment, setupSessionBridge: true);
builder.Services.AddLegacyAuthentication();

builder.Services.AddOpenApi();
var reportServerAddress = builder.Configuration["ReportServer:Address"] 
                          ?? throw new InvalidOperationException("ReportServer:Address");

builder.Services.AddReportServerRpcClient(reportServerAddress);
builder.Services.AddScoped<TerminalTool>();

var kernelBuilder = builder.Services.AddKernel();
kernelBuilder.Plugins.AddFromType<TerminalTool>();

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
    .UseAuthenticatedSession()
    ;
app.MapAuthenticationEndpoints();
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
namespace RsMcpServer.Web
{
    public partial class Program { }
}

