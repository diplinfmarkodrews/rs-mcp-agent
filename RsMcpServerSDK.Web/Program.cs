using OpenAPISwaggerUI;
using ReportServerPort;
using ReportServerRPCClient.Extensions;
using RsMCPServerSDK.Web.Services;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using ReportServerRPCClient.DTOs.Authentication;

// Make the Program accessible to the test project
[assembly: InternalsVisibleTo("TestRsMcpServer")]

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
var reportServerAddress = builder.Configuration["ReportServer:Address"] ?? "http://localhost:1099/";
builder.Services.AddReportServerRpcClient(reportServerAddress); 
builder.Services.AddScoped<McpReportServer>();
// builder.Services.AddHostedService<McpServerHostedService>();
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<McpReportServer>();
    // .WithToolsFromAssembly(typeof(McpReportServer).Assembly);

var app = builder.Build();


// Configure the HTTP request pipeline.
app.UseOpenApi();
app.UseHttpsRedirection();
app.UseOpenAPISwaggerUI();
app.MapPost("/rs-authenticate", async ([FromBody]AuthenticationRequest request, IReportServerClient rsClient) =>
    {
        if (string.IsNullOrWhiteSpace(request.user) || string.IsNullOrWhiteSpace(request.password))
        {
            return Results.BadRequest("Username and password must be provided.");
        }
        var rsResponse = await rsClient.AuthenticateAsync(request.user, request.password);
        // Todo properly handle authentication response
        // later, register clients
        // if (!rsResponse.IsSuccess)
        // {
        //     return Results.Conflict(rsResponse);
        // }
        return Results.Ok(rsResponse);
    })
    .WithName("authenticate");

app.MapMcp()
    // .WithHttpLogging()
    .WithDescription("MCP Server for Report Server RPC Client")
    .WithOpenApi()
    ;
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();

public record AuthenticationRequest(string user, string password);
