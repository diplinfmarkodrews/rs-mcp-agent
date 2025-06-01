using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddOllama("ollama")
    .WithGPUSupport()
    .WithDataVolume();

var chat = ollama.AddModel("chat",  
    builder.Configuration["Ollama:Model"] ?? "mistral-nemo:12b");
var embeddings = ollama.AddModel("embeddings", builder.Configuration["Ollama:EmbeddingModel"] ?? "all-minilm");

var vectorDB = builder.AddQdrant("vectordb")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var mcpServer = builder.AddProject<Projects.RsMcpServerSDK_Web>("rs-mcp-server");

var webApp = builder.AddProject<Projects.RSChatApp_Web>("aichatweb-app");
webApp
    .WithReference(chat)
    .WithReference(embeddings)
    .WithReference(mcpServer)
    .WaitFor(chat)
    .WaitFor(mcpServer)
    .WaitFor(embeddings);
webApp
    .WithReference(vectorDB)
    .WaitFor(vectorDB);

// Todo: Add reference to ReportServer(Mock)

builder.Build().Run();
