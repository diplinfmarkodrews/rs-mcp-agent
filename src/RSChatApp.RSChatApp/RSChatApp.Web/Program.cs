using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.AI;
using Microsoft.IdentityModel.Protocols.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using RSChatApp.Infrastructure.Extensions;
using RSChatApp.Infrastructure.ReportServer.Clients;
using RSChatApp.Infrastructure.UserInteraction;
using RSChatApp.Shared.Infrastructure.Mcp.Browser.Configuration;
using RSChatApp.Shared.Infrastructure.Mcp.Browser.Mcp;
using RSChatApp.Shared.Infrastructure.Mcp.Browser.Middleware;
using RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.Processing;
using RSChatApp.Shared.Infrastructure.Mcp.Extensions;
using RSChatApp.Shared.Infrastructure.Mcp.Ingestion.Services;
using RSChatApp.Shared.Infrastructure.Mcp.ReportServer.Mcp;
using RSChatApp.Web.Components;
using RSChatApp.Web.Configuration;
using RSChatApp.Web.Extensions;
using RSChatApp.Web.Hubs;
using RSChatApp.Web.Mcp.Tools;
using RSChatApp.Web.Models.Auth;
using RSChatApp.Web.Models.Terminal;
using RSChatApp.Web.Services.Chat;
using RSChatApp.Web.Services.Chat.Tools;
using RSChatApp.Web.Services.ChatHistory;
using RSChatApp.Web.Services.Terminal;
using RSChatApp.Web.Services.Terminal.Drivers;
using RSChatApp.Web.Services.UserConfirmation;
using RSChatApp.Web.Storage;
using RsMcpServer.Identity.Models.Requests;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);
// Configure logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
    logging.AddConsole();
    logging.AddDebug();

    // Serilog has its own minimum level; set it to Debug so file logging can capture Debug events.
    // The effective filtering is still controlled by the Microsoft "Logging" configuration above.
    logging.AddSerilog(
        new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.File("Logs/rschatapp-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger(),
        dispose: true);
});
builder.Services.AddOptions();
// Configure global JSON serialization options for Blazor components
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.Converters.Add(new ChatMessageConverter());
});
builder.Services.Configure<BrowserInstanceConfiguration>(
    builder.Configuration.GetSection(nameof(BrowserInstanceConfiguration)));
builder.Services.Configure<OpenAIPromptExecutionSettings>(
    builder.Configuration.GetSection(nameof(OpenAIPromptExecutionSettings)));
builder.Services.Configure<OpenAIPromptExecutionSettings>(
    config =>
    {
        config.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();
    });
builder.Services.AddOptions<OpenAIPromptExecutionSettings>();
OpenAiSettings openAiSettings = new();
builder.Configuration.GetSection(nameof(OpenAiSettings))
    .Bind(openAiSettings);

openAiSettings.SetApiKey();

var reportServerUrl = builder.Configuration.GetValue<string>("ReportServer:Url")
                      ?? throw new InvalidConfigurationException("ReportServer:Url is missing");

McpClientSettings mcpClientSettings = new();
builder.Configuration.GetSection(nameof(McpClientSettings))
    .Bind(mcpClientSettings);

builder.Services.AddHealthChecks();

// Add Keycloak authentication
// Add custom authentication service
builder.Services.AddInfrastructureServices();
builder.Services.AddCustomAuthenticationService();
// builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment, setupSessionBridge: false);
var sessionTimeoutMinutes = builder.Configuration.GetValue<int?>("SessionCookieSettings:IdleTimeout") ?? 15;
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "RsMcpServer.AuthCookie";
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(sessionTimeoutMinutes);
    });
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(sessionTimeoutMinutes);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddCors(setup =>
{
    setup.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // In development, be permissive for testing
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            var allowedOrigins = builder.Configuration
                                     .GetSection("AllowedCorsOrigins")
                                     .Get<string[]>() ?? Array.Empty<string>();
            
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

builder.Services.AddHttpClient(RsMcpServerHttpClientName.ClientName, client =>
{
    client.BaseAddress = new Uri(builder.Configuration["RsMcpServer:Url"] 
                                 ?? throw new InvalidConfigurationException("RsMcpServer:Url"));
    client.DefaultRequestHeaders.Add("Accept", "text/json, application/json");    
})
    .AddStandardResilienceHandler(); // Only used without aspire

builder.Services.AddBrowserInstance(reportServerUrl);

// Static content sources (configurable additional static file roots + index/file stores)
builder.Services.AddStaticContentServices(builder.Configuration);

// create the MCP client at startup via BuildServiceProvider
// and register tool functions into KernelPluginCollection.
// This is ugly, but works! Couldnt make the Pluginregistration work in HostedService
using var scopedServiceProvider = builder.Services.BuildServiceProvider();
await using IMcpClient mcpClientRS = await McpClientFactory.CreateAsync(
    new SseClientTransport(
        new SseClientTransportOptions
        {
            Name = RsMcpServerHttpClientName.ClientName,
            Endpoint = new Uri(builder.Configuration["RsMcpServer:Url"]
                               ?? throw new InvalidConfigurationException("RsMcpServer:Url")),
        },
        httpClient: scopedServiceProvider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(RsMcpServerHttpClientName.ClientName),
        loggerFactory: scopedServiceProvider
            .GetRequiredService<ILoggerFactory>()
    ));
var toolsRs = await mcpClientRS.ListToolsAsync();

builder.Services.AddScoped((serviceProvider) =>
{
    var startupLogger = serviceProvider.GetRequiredService<ILogger<Program>>();

    startupLogger.LogInformation("Register RsMcpClient with toolCalls: {toolCalls}",
        new StringBuilder().AppendJoin(", ", toolsRs.Select(t => t.Name)));

    KernelPluginCollection pluginCollection = [];
    pluginCollection.AddFromType<BrowserTool>("BrowserTool", serviceProvider);
    pluginCollection.AddFromFunctions("TerminalTool",
        toolsRs.Select(aiFunction => aiFunction.AsKernelFunction()));
    pluginCollection.AddFromType<TerminalResource>("TerminalResource", serviceProvider);
    return pluginCollection;
});

// register a base KernelPluginCollection, then let a hosted service
// connect to RsMcpServer and insert the MCP tool KernelFunctions into this collection.
// builder.Services.AddSingleton<KernelPluginCollection>((serviceProvider) =>
// {
//     KernelPluginCollection pluginCollection = [];
//     return pluginCollection;
// });
// builder.Services.AddHostedService<RsMcpToolRegistrationHostedService>();

// Register IEnumerable<KernelPlugin> for the Kernel constructor
builder.Services.AddScoped<IEnumerable<KernelPlugin>>((serviceProvider) => {
    var pluginCollection = serviceProvider.GetRequiredService<KernelPluginCollection>();
    return pluginCollection;
});
// 

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add service defaults (OpenTelemetry, health checks, etc.) - commented out for explicit endpoint configuration
builder.AddServiceDefaults();

if (string.IsNullOrEmpty(openAiSettings.Model) == false)
{
    if (openAiSettings.IsValid() == false)
        throw new InvalidConfigurationException("OpenAI API key not set properly in env.");
    
    builder.Services.AddOpenAIChatClient(
        openAiSettings,
        openTelemetryConfig: f => f.EnableSensitiveData = false);
}
else
{
    builder.AddOllamaApiClient("chat")
        .AddChatClient()
        .UseFunctionInvocation()
        .UseOpenTelemetry(configure: c =>
            c.EnableSensitiveData = builder.Environment.IsDevelopment());
}
// Create local EmbeddingClient with Ollama
builder.AddOllamaApiClient("embeddings")
    .AddEmbeddingGenerator(); // Used internally by IEmbeddingGenerator

builder.AddQdrantClient("vectordb");

// User interaction / confirmations (scoped per Blazor circuit)

builder.Services.AddScoped<IWaitForUserInteraction<TerminalConfirmRequest, UserConfirmationResult>, 
    WaitForUserInteraction<TerminalConfirmRequest, UserConfirmationResult>>();
builder.Services.AddScoped<IFunctionInvocationFilter, UserConfirmInvocationFilter>();

builder.Services.AddScoped<Kernel>((serviceProvider)=> {
    // Create a per-scope plugin collection so BrowserTool is constructed within a valid
    // request/circuit scope (HttpContext + Session available), while MCP tool functions
    // are shared via the singleton KernelPluginCollection.
    var sharedPlugins = serviceProvider.GetRequiredService<KernelPluginCollection>();
    KernelPluginCollection pluginCollection = [];
    foreach (var plugin in sharedPlugins)
        pluginCollection.Add(plugin);
    
    var kernel = new Kernel(serviceProvider, pluginCollection);
    foreach (var filter in serviceProvider.GetServices<IFunctionInvocationFilter>())
        kernel.FunctionInvocationFilters.Add(filter);
    
    return kernel;
});

builder.Services.AddPromptServices();
builder.Services.AddIngestionAndSemanticSearch();

builder.Services.AddScoped<AuthenticationTool>();
builder.Services.AddScoped<UserConfirmedTerminalTool>();

// Tool call processing services
builder.Services.AddSingleton<ToolRegistry>();
builder.Services.AddScoped<ToolCallProcessor>();

// Browser storage abstraction - choose LocalStorage or SessionStorage
if (builder.Environment.IsDevelopment())
    builder.Services.AddScoped<IProtectedBrowserStorage, ProtectedLocalStorageAdapter>();
else // Use session storage in production, data is dropped after browser is closed or session expires
    builder.Services.AddScoped<IProtectedBrowserStorage, ProtectedSessionStorageAdapter>();

// BrowserStorages
builder.Services.AddScoped<IStorage<List<ChatMessage>>, ChatHistoryStorage>()
    .AddScoped<IStorage<List<TerminalInstance>>, TerminalInstanceStorage>();

// Terminal services
builder.Services.AddScoped<ITerminalManager, TerminalManagerService>();
builder.Services.AddScoped<RsTerminalDriver>();
builder.Services.AddScoped<JsTerminalDriver>();
builder.Services.AddScoped<TerminalDriverFactory>();

builder.Services.AddControllers();

var app = builder.Build();

// Add session middleware before browser middleware
app.UseRouting();
app.UseCors(); // Enable CORS middleware
app.UseAuthentication();
app.UseStaticFiles();

// Add configured additional static content roots (e.g. downloaded ReportServer scripts)
app.UseConfiguredStaticContent();

app.UseAntiforgery();

app.UseSession();
app.UseMiddleware<BrowserSessionMiddleware>();

app.MapDefaultEndpoints();
app.MapControllers();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapHealthChecks("/health");

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.MapHub<BrowserStreamHub>("/browserstreamhub");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    // .AddAdditionalAssemblies()
    ;

// Add authentication debug endpoint for development
if (app.Environment.IsDevelopment())
{
    // Add a diagnostic endpoint to check loaded tools
    app.MapGet("/debug/tools", (Kernel kernel) =>
    {
        var plugins = kernel.Plugins.Select(p => new
        {
            Name = p.Name,
            FunctionCount = p.Count(),
            Functions = p.Select(f => new {
                Name = f.Name,
                Description = f.Description
            }).ToList()
        }).ToList();

        return Results.Json(new { TotalPlugins = plugins.Count, Plugins = plugins });
    });
    
    // app.MapGet("/debug/auth-config", (IConfiguration config) =>
    // {
    //     return Results.Ok(new
    //     {
    //         Authority = config["Keycloak:Authority"],
    //         ClientId = config["Keycloak:ClientId"],
    //         HasClientSecret = !string.IsNullOrEmpty(config["Keycloak:ClientSecret"]),
    //         RequireHttpsMetadata = config["Keycloak:RequireHttpsMetadata"],
    //         Scopes = config.GetSection("Keycloak:Scopes").Get<string[]>(),
    //         ReportServerUrl = config["ReportServer:Url"]
    //     });
    // });

    // app.MapGet("/debug/keycloak-health", async (IHttpClientFactory httpClientFactory) =>
    // {
    //     try
    //     {
    //         var httpClient = httpClientFactory.CreateClient();
    //         var keycloakAuthority = app.Configuration["Keycloak:Authority"];
    //         var response = await httpClient.GetAsync($"{keycloakAuthority}/.well-known/openid_configuration");

    //         if (response.IsSuccessStatusCode)
    //         {
    //             var content = await response.Content.ReadAsStringAsync();
    //             return Results.Ok(new { Status = "Healthy", Response = content });
    //         }
    //         else
    //         {
    //             return Results.Ok(new { Status = "Unhealthy", StatusCode = response.StatusCode, Reason = response.ReasonPhrase });
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         return Results.Ok(new { Status = "Error", Message = ex.Message });
    //     }
    // });
}

    // By default, we ingest PDF files from the /wwwroot/Data directory. You can ingest from
    // other sources by implementing IIngestionSource.
    // Important: ensure that any content you ingest is trusted, as it may be reflected back
    // to users or could be a source of prompt injection risk.
    await DataIngestor.IngestDataAsync(
        app.Services,
        new PDFDirectorySource(Path.Combine(builder.Environment.WebRootPath, "Data")));

    // Ingest text files from the /wwwroot/Data directory.
    // Only supports .txt files, no subfolders
    await DataIngestor.IngestDataAsync(
        app.Services,
        new TextDirectorySource(Path.Combine(builder.Environment.WebRootPath, "Data")));

    app.Run();

namespace RSChatApp.Web
{
    public partial class Program { }
}

