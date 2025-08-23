using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);
bool hasGpu = builder.Configuration.GetValue<bool>("Ollama:Gpu");
var ollama = hasGpu 
    ? builder.AddOllama("ollama")
        .WithGPUSupport()
        .WithDataVolume()
    : builder.AddOllama("ollama")
        .WithDataVolume();

bool deployMainModel = string.IsNullOrEmpty(builder.Configuration["Ollama:Model"]) == false;
IResourceBuilder<OllamaModelResource> chat = null;
if (deployMainModel)
{
    chat = ollama.AddModel("chat",  
    builder.Configuration["Ollama:Model"]);
}

var embeddings = ollama.AddModel("embeddings", 
    builder.Configuration["Ollama:EmbeddingModel"] ?? "all-minilm");

var vectorDB = builder.AddQdrant("vectordb")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .PublishAsConnectionString();

var mcpServer = builder.AddProject<Projects.RsMcpServer_Web>("rs-mcp-server");
var webApp = builder.AddProject<Projects.RSChatApp_Web>("aichatweb-app");

if (deployMainModel)
{
    webApp
        .WithReference(chat)
        .WithReference(embeddings)
        .WithReference(mcpServer)
        .WithReference(vectorDB)
        .WaitFor(chat)
        .WaitFor(embeddings)
        .WaitFor(mcpServer)
        .WaitFor(vectorDB);
}
else
{
    webApp
        .WithReference(embeddings)
        .WithReference(mcpServer)
        .WithReference(vectorDB)
        .WaitFor(embeddings)
        .WaitFor(mcpServer)
        .WaitFor(vectorDB);
}

builder.Build().Run();
