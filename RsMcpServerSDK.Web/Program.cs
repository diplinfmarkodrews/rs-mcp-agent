using OpenAPISwaggerUI;
using ReportServerPort;
using ReportServerRPCClient.Extensions;
using RsMCPServerSDK.Web.Services;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using ReportServerRPCClient.DTOs.Authentication;
using RsMCPServerSDK.Web.Infrastructure;

// Make the Program accessible to the test project
[assembly: InternalsVisibleTo("TestRsMcpServer")]

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.Plugins.AddFromType<McpReportServer>();

var kernel = kernelBuilder.Build();

builder.Services.AddOpenApi();
var reportServerAddress = builder.Configuration["ReportServer:Address"] ?? "http://localhost:1099/";
builder.Services.AddReportServerRpcClient(reportServerAddress); 
builder.Services.AddScoped<McpReportServer>();
// builder.Services.AddHostedService<McpServerHostedService>();
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools(kernel.Plugins);

var app = builder.Build();


// Configure the HTTP request pipeline.
app.UseOpenApi();
app.UseHttpsRedirection();
app.UseOpenAPISwaggerUI();
// app.UseMiddleware<SessionAuthorizationMiddleware>();

app.MapPost("/rs-authenticate", async ([FromBody]AuthenticationRequest request, IReportServerClient rsClient) =>
    {
        if (string.IsNullOrWhiteSpace(request.user) || string.IsNullOrWhiteSpace(request.password))
        {
            return Results.BadRequest("Username and password must be provided.");
        }
        var rsResponse = await rsClient.AuthenticateAsync(request.user, request.password);
        // Todo properly handle authentication response
        // later, register clients
        if (rsResponse.IsSuccess)
        {
            // Register RsSessionId, on clientSessionId in CookieContainerProvider
        }
        return Results.Ok(rsResponse);
    })
    .WithName("authenticate");

app.MapMcp()
    .WithHttpLogging(HttpLoggingFields.All)
    .WithDescription("MCP Server for the Report Server")
    .WithOpenApi()
    ;
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();

public record AuthenticationRequest(string user, string password);
