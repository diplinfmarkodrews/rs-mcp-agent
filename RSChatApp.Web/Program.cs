using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using RSChatApp.Web.Components;
using RSChatApp.Web.Models.Ingestion;
using RSChatApp.Web.Services.Ingestion;
using RSChatApp.Web.Services.SemanticSearch;
using RSChatApp.Web.Extensions;
using RsMcpServer.Identity.Extensions;
using RsMcpServer.Identity.Middleware;

var builder = WebApplication.CreateBuilder(args);
// builder.Configuration.AddJsonFile("prompts.json", optional: true, reloadOnChange: true);
builder.Services.AddOptions();
OpenAIPromptExecutionSettings promptExecutionSettings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};
builder.Configuration.GetSection(nameof(OpenAIPromptExecutionSettings))
    .Bind(promptExecutionSettings);

// Add Keycloak authentication
builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);

// Add custom authentication service
builder.Services.AddCustomAuthenticationService();

builder.Services.AddHttpClient("RsMcpServer", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["RsMcpServer:Address"] 
                                 ?? throw new InvalidOperationException("RsMcpServer:Address"));
    client.DefaultRequestHeaders.Add("Accept", "text/json, application/json");
}).AddStandardResilienceHandler(); // Only used without aspire 

builder.Services.AddKernel();

var scopedServiceProvider = builder.Services.BuildServiceProvider()
    .CreateScope()
    .ServiceProvider;

// Creating McpClient with SSE transport
await using IMcpClient mcpClientRS = await McpClientFactory.CreateAsync(
    new SseClientTransport(
        new SseClientTransportOptions
        {
            Name = "RsMcpServer",
            Endpoint = new Uri(builder.Configuration["RsMcpServer:Address"] 
                               ?? throw new InvalidOperationException("RsMcpServer:Address")),
        },
        httpClient: scopedServiceProvider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient("RsMcpServer"),
        loggerFactory: scopedServiceProvider
            .GetRequiredService<ILoggerFactory>()
    ));

// Create an MCPClient for the Sequential Thinking server
await using IMcpClient mcpClientSeqThinking = await McpClientFactory.CreateAsync(new StdioClientTransport(new()
{
    Name = "SequentialThinking",
    Command = "npx",
    Arguments = ["-y", "@modelcontextprotocol/server-sequential-thinking"],
}));

var toolsRs = await mcpClientRS.ListToolsAsync();
var toolsSeqThinking = await mcpClientSeqThinking.ListToolsAsync();
var kernelBuilder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0001
kernelBuilder.Plugins.AddFromFunctions("RsMcpServer", toolsRs.Select(aiFunction => aiFunction.AsKernelFunction()));
kernelBuilder.Plugins.AddFromFunctions("SequentialThinking", toolsSeqThinking.Select(aiFunction => aiFunction.AsKernelFunction()));
#pragma warning restore SKEXP0001

//TODO: Register Chat and Embedding clients with the kernel
var kernel = kernelBuilder.Build();
builder.Services.AddSingleton(kernel);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add service defaults (OpenTelemetry, health checks, etc.) - commented out for explicit endpoint configuration
builder.AddServiceDefaults();

builder.AddOllamaApiClient("chat")
    .AddChatClient()
    .UseFunctionInvocation() 
    .UseKernelFunctionInvocation()
    .UseOpenTelemetry(configure: c =>
        c.EnableSensitiveData = builder.Environment.IsDevelopment());

builder.AddOllamaApiClient("embeddings")
    .AddEmbeddingGenerator(); // Used internally by IEmbeddingGenerator

builder.AddQdrantClient("vectordb");
    
builder.Services.AddQdrantCollection<Guid, IngestedChunk>("data-rschatapp-chunks");
builder.Services.AddQdrantCollection<Guid, IngestedDocument>("data-rschatapp-documents");
builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();
builder.Services.AddControllers();
var app = builder.Build();

app.MapDefaultEndpoints();
app.MapControllers();
// Use Keycloak authentication
app.UseAuthentication();
app.UseSession();
app.UseAuthenticatedSession();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

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

