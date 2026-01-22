using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.AI;
using Microsoft.IdentityModel.Protocols.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using RSChatApp.Infrastructure.UserInteraction;
using RSChatApp.Infrastructure.Extensions;
using RSChatApp.Infrastructure.ReportServer.Clients;
using RSChatApp.Mcp.Browser.Configuration;
using RSChatApp.Mcp.Browser.Extensions;
using RSChatApp.Mcp.Browser.Middleware;
using RSChatApp.Mcp.Browser.Tools;
using RSChatApp.Web.Components;
using RSChatApp.Web.Configuration;
using RSChatApp.Web.Extensions;
using RSChatApp.Web.Hubs;
using RSChatApp.Web.Mcp.Tools;
using RSChatApp.Web.Models.Ingestion;
using RSChatApp.Web.Models.Terminal;
using RSChatApp.Web.Services.ChatHistory;
using RSChatApp.Web.Services.Ingestion;
using RSChatApp.Web.Services.SemanticSearch;
using RSChatApp.Web.Services.Terminal;
using RSChatApp.Web.Services.Terminal.Drivers;
using RSChatApp.Web.Services.UserConfirmation;
using RSChatApp.Web.Services.Chat;
using RSChatApp.Web.Services.Chat.Tools;
using RSChatApp.Web.Storage;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
// builder.Configuration.AddJsonFile("prompts.json", optional: true, reloadOnChange: true);
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddSerilog(new LoggerConfiguration()
        .WriteTo.File("Logs/rschatapp-.log", rollingInterval: RollingInterval.Day)
        .CreateLogger());
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});
builder.Services.AddOptions();
builder.Services.Configure<BrowserInstanceConfiguration>(
    builder.Configuration.GetSection(nameof(BrowserInstanceConfiguration)));
builder.Services.Configure<OpenAIPromptExecutionSettings>(
    builder.Configuration.GetSection(nameof(OpenAIPromptExecutionSettings)));
builder.Services.Configure<OpenAIPromptExecutionSettings>(
    config =>
    {
        config.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();
    });

OpenAISettings openAISettings = new();
builder.Configuration.GetSection(nameof(OpenAISettings))
    .Bind(openAISettings);

openAISettings.SetApiKey();

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
            var allowedOrigins = builder.Configuration.GetSection("AllowedCorsOrigins").Get<string[]>()
                               ?? Array.Empty<string>();
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

builder.Services.AddSingleton((serviceProvider) =>
{
    var startupLogger = serviceProvider.GetRequiredService<ILogger<Program>>();

    startupLogger.LogInformation("Register RsMcpClient with toolCalls: {toolCalls}", 
        new StringBuilder().AppendJoin(", ", toolsRs.Select(t => t.Name)));
    
    KernelPluginCollection pluginCollection = [];
    pluginCollection.AddFromType<BrowserTool>("BrowserTool", serviceProvider);
    pluginCollection.AddFromFunctions(RsMcpServerHttpClientName.ClientName, 
        toolsRs.Select(aiFunction => aiFunction.AsKernelFunction()));
    return pluginCollection;
});

// Register IEnumerable<KernelPlugin> for the Kernel constructor
builder.Services.AddSingleton<IEnumerable<KernelPlugin>>((serviceProvider) => {
    var pluginCollection = serviceProvider.GetRequiredService<KernelPluginCollection>();
    return pluginCollection;
});


// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure global JSON serialization options for Blazor components (including ProtectedBrowserStorage)
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.Converters.Add(new ChatMessageConverter());
});

// Add service defaults (OpenTelemetry, health checks, etc.) - commented out for explicit endpoint configuration
builder.AddServiceDefaults();

if (string.IsNullOrEmpty(openAISettings.Model) == false)
{
    if (openAISettings.IsValid() == false)
        throw new InvalidConfigurationException("OpenAI API key not set properly in env.");
    
    builder.Services.AddOpenAIChatClient(openAISettings.Model,
        new Uri(openAISettings.Url),
        openAISettings.ApiKey,
        openTelemetryConfig:(f) => f.EnableSensitiveData = false
    );       
}
else
{
    builder.AddOllamaApiClient("chat")
        .AddChatClient()
        .UseFunctionInvocation()
        .UseKernelFunctionInvocation()
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
    KernelPluginCollection pluginCollection = serviceProvider.GetRequiredService<KernelPluginCollection>();
    var kernel = new Kernel(serviceProvider, pluginCollection);

    foreach (var filter in serviceProvider.GetServices<IFunctionInvocationFilter>())
    {
        kernel.FunctionInvocationFilters.Add(filter);
    }
    return kernel;
});

builder.Services.AddQdrantCollection<Guid, IngestedChunk>("data-rschatapp-chunks");
builder.Services.AddQdrantCollection<Guid, IngestedDocument>("data-rschatapp-documents");
builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();
builder.Services.AddScoped<SemanticSearchTool>();
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
                Name = f.Name ?? "Unknown",
                Description = f.Description ?? "No description"
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

    //TODO: Refactor into Service, make ingestion available at runtime to user
    //
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

