using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddOllama("ollama")
    .WithGPUSupport()
    .WithDataVolume();

var chat = ollama.AddModel("chat",  
    builder.Configuration["Ollama:Model"] ?? "mistral-nemo:12b");
var embeddings = ollama.AddModel("embeddings", 
    builder.Configuration["Ollama:EmbeddingModel"] ?? "all-minilm");

var vectorDB = builder.AddQdrant("vectordb")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var mcpServer = builder.AddProject<Projects.RsMcpServer_Web>("rs-mcp-server");

var webApp = builder.AddProject<Projects.RSChatApp_Web>("aichatweb-app");

webApp
    .WithReference(chat)
    .WithReference(embeddings)
    .WithReference(mcpServer)
    .WithReference(vectorDB)
    .WaitFor(chat)
    .WaitFor(embeddings)
    .WaitFor(mcpServer)
    .WaitFor(vectorDB);

builder.Build().Run();
