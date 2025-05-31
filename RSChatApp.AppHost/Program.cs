var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddOllama("ollama")
    .WithDataVolume();
var chat = ollama.AddModel("chat", "llama3.2");
var embeddings = ollama.AddModel("embeddings", "all-minilm");

var vectorDB = builder.AddQdrant("vectordb")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var mcpServer = builder.AddProject<Projects.RsMcpServerSDK_Web>("rs-mcp-server");

var webApp = builder.AddProject<Projects.RSChatApp_Web>("aichatweb-app");
webApp
    .WithReference(chat)
    .WithReference(embeddings)
    // .withReference(mcpServer)
    .WaitFor(chat)
    .WaitFor(embeddings)
    .WaitFor(mcpServer);
webApp
    .WithReference(vectorDB)
    .WaitFor(vectorDB);

// Todo: Add reference to ReportServer(Mock)

builder.Build().Run();
