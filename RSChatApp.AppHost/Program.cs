using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

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
if (builder.Environment.IsDevelopment())
{
    // In development, run the Java sidecar directly (requires JDK installed locally)
    var javaRsSidecarExec = builder.AddExecutable("java-rs-sidecar", "java",
        "./rs-rest-sidecar", "-jar", "target/rs-rest-sidecar-1.0.0.jar")
        .WithHttpEndpoint(port: 8091, name: "http")
        .WithHealthCheck("/api/health");

    mcpServer        
        .WithEnvironment("Rs_Rest__BaseUrl", "http://localhost:8091")
        .WaitFor(javaRsSidecarExec);
}
else
{
    // In production, run the Java sidecar as a container (requires image in registry)
    var javaRsSidecarContainer = builder.AddContainer("rs-rest-sidecar", "openjdk", "17-jre-slim")
        .WithHttpHealthCheck("/api/health")
        .WithBindMount("./rs-rest-sidecar/target", "/app")
        .WithEntrypoint("java")
        .WithArgs("-jar", "rs-rest-sidecar-1.0.0.jar")
        .WithEnvironment("SERVER_PORT", "8091")
        .WithEnvironment("REPORTSERVER_BASE_URL", "http://localhost:8090")        
        .WithHttpEndpoint(8091, 8091, "http");

    mcpServer                
        .WithEnvironment("Rs_Rest__BaseUrl", "http://localhost:8091")
        .WaitFor(javaRsSidecarContainer);

}

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
