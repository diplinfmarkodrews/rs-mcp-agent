using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using RSChatApp.Web.Components;
using RSChatApp.Web.Services;
using RSChatApp.Web.Services.Ingestion;
using RsMcpServer.Identity.Extensions;
using RsMcpServer.Identity.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Keycloak authentication
builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);

builder.Services.AddHttpClient("RsMcpServer", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["RsMcpServer:Address"] 
                                 ?? throw new InvalidOperationException("RsMcpServer:Address"));
    client.DefaultRequestHeaders.Add("Accept", "text/json, application/json");
});
#region McpClientConfiguration
// Creating McpClient with SSE transport
await using IMcpClient mcpClient = await McpClientFactory.CreateAsync(
    new SseClientTransport(
        new SseClientTransportOptions
        {
            Endpoint = new Uri(builder.Configuration["RsMcpServer:Address"] 
                               ?? throw new InvalidOperationException("RsMcpServer:Address")),
        },
        httpClient: builder.Services.BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient("RsMcpServer"),
        loggerFactory: builder.Services.BuildServiceProvider()
            .GetRequiredService<ILoggerFactory>()
    ));
builder.Services.AddSingleton(mcpClient);

var tools = await mcpClient.ListToolsAsync();
IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0001
kernelBuilder.Plugins.AddFromFunctions("Tools", tools.Select(aiFunction => aiFunction.AsKernelFunction()));
#pragma warning restore SKEXP0001
#endregion

Kernel kernel = kernelBuilder.Build();
builder.Services.AddSingleton(kernel);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

builder.AddServiceDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton(new OpenAIPromptExecutionSettings
{
    MaxTokens = 4096,
    Temperature = 0.7f,
    TopP = 0.9f,
    FrequencyPenalty = 0.0f,
    PresencePenalty = 0.0f,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
});

var ollamaConnectionString = builder.Configuration["Ollama:Address"];
builder.AddOllamaApiClient("chat"
    , config =>
    {
        config.Endpoint = !string.IsNullOrEmpty(ollamaConnectionString) 
            ? new Uri(ollamaConnectionString) 
            : throw new InvalidProgramException("Ollama API url is not configured.");
        config.Models = [builder.Configuration["Ollama:Model"] ?? "llama3"];
    })
    .AddChatClient()
    .UseFunctionInvocation() 
    .UseKernelFunctionInvocation()
    .UseOpenTelemetry(configure: c =>
        c.EnableSensitiveData = builder.Environment.IsDevelopment());

builder.AddOllamaApiClient("embeddings",config =>
    {
        config.Endpoint = !string.IsNullOrEmpty(ollamaConnectionString) 
            ? new Uri(ollamaConnectionString) 
            : throw new InvalidProgramException("Ollama API url is not configured.");
        config.Models = [builder.Configuration["Ollama:EmbeddingModel"] ?? "llama3.2:1b"];
    })
    .AddEmbeddingGenerator();

builder.AddQdrantClient("vectordb", config =>
{
    var qdrantAddress = builder.Configuration["Qdrant:Address"];
    config.Endpoint = !string.IsNullOrEmpty(qdrantAddress)
        ? new Uri(qdrantAddress)
        : throw new InvalidProgramException("Qdrant url is not configured.");
    config.Key = builder.Configuration["Qdrant:ApiKey"];
});
    
builder.Services.AddQdrantCollection<Guid, IngestedChunk>("data-rschatapp-chunks");
builder.Services.AddQdrantCollection<Guid, IngestedDocument>("data-rschatapp-documents");
builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Use Keycloak authentication
app.UseAuthentication();
app.UseSession();
app.UseAuthenticationSession();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization(); // Require authentication for the main app

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


// record LoginRequest(string Username, string Password);