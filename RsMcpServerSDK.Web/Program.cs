using MCPServerSDK.Web.Services;
using ReportServerPort;
using ReportServerRPCClient.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
var reportServerAddress = builder.Configuration["ReportServer:Address"] ?? "http://localhost:1099/";
builder.Services.AddReportServerRpcClient(reportServerAddress); 
builder.Services.AddScoped<McpReportServer>();
// builder.Services.AddHostedService<McpServerHostedService>();
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly(typeof(McpReportServer).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapGet("/rs-authenticate", async (string user, string password, IReportServerClient rsClient) =>
    {
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
        {
            return Results.BadRequest("Username and password must be provided.");
        }
        var rsResponse = await rsClient.AuthenticateAsync(user, password);
        // Todo properly handle authentication response
        // later, register clients
        return Results.Ok(rsResponse);
    })
    .WithName("authenticate");

app.MapMcp()
    // .WithHttpLogging()
    .WithOpenApi()
    ;

app.Run();
