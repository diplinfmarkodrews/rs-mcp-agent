using Projects;
using RSChatApp.AppHost.Factories;

var builder = DistributedApplication.CreateBuilder(args);

var ollamaHost = builder.AddOllamaHost();
var ollamaModels = builder.AddOllamaModels(ollamaHost);
var vectorDb = builder.AddQdrant("vectordb")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .PublishAsConnectionString();

var postGreSql = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .PublishAsConnectionString();

var mcpServer = builder.AddProject<RsMcpServer_Api>("rs-mcp-server");
var webApp = builder.AddProject<RSChatApp_Web>("aichatweb-app");

webApp.WithReference(mcpServer)
    .WithReference(vectorDb)
    .WithReference(postGreSql)
    .WaitFor(mcpServer)
    .WaitFor(vectorDb)
    .WaitFor(postGreSql);

foreach (var model in ollamaModels)
{
    webApp.WithReference(model.Item1);
    if(model.Item2)
        webApp.WaitFor(model.Item1);
}

builder.Build().Run();
