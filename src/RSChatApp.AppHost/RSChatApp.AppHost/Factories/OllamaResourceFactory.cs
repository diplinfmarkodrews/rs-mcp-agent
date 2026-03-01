using Microsoft.Extensions.Configuration;

namespace RSChatApp.AppHost.Factories;

internal static class OllamaResourceFactory
{
    internal static IResourceBuilder<OllamaResource> AddOllamaHost(this IDistributedApplicationBuilder builder)
    {
        bool hasGpu = builder.Configuration.GetValue<bool>("Ollama:Gpu");
        bool hasEndpoint = string.IsNullOrEmpty(builder.Configuration["Ollama:Url"]) == false
            && Uri.IsWellFormedUriString(builder.Configuration["Ollama:Url"], UriKind.Absolute);

        var ollama = builder.AddOllama("ollama")
            .WithImageTag("latest")
            .WithDataVolume();
        
        if (hasEndpoint)
        {
            ollama.WithUrl(builder.Configuration["Ollama:Url"]!);
        }

        if (hasGpu)
        {
            ollama.WithGPUSupport();
        }
        
        return ollama;
    }

    internal static IEnumerable<(IResourceBuilder<OllamaModelResource>, bool)> AddOllamaModels(this IDistributedApplicationBuilder builder, IResourceBuilder<OllamaResource> ollamaHost)
    {
        var modelConfigs = builder.Configuration.GetSection("Ollama:Models").GetChildren();
        foreach (var model in modelConfigs)
        {
            if (!string.IsNullOrWhiteSpace(model["ConnectionName"]) && !string.IsNullOrWhiteSpace(model["Name"]))
                yield return (ollamaHost.AddModel(model["ConnectionName"]!,
                    model["Name"]!), model.GetValue<bool>("Required")); 
        }
        
    }
}