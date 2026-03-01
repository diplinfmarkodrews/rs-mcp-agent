using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using RSChatApp.Shared.Infrastructure.Mcp.Browser.Configuration;
using RSChatApp.Shared.Infrastructure.Mcp.Browser.Implementations;
using RSChatApp.Shared.Infrastructure.Mcp.Browser.Interfaces;
using RSChatApp.Shared.Infrastructure.Mcp.Ingestion.Models;
using RSChatApp.Shared.Infrastructure.Mcp.Ingestion.Services;
using RSChatApp.Shared.Infrastructure.Mcp.SemanticSearch.Mcp;
using RSChatApp.Shared.Infrastructure.Mcp.SemanticSearch.Services;
using RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Configuration;
using RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Mcp;
using RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Services;

namespace RSChatApp.Shared.Infrastructure.Mcp.Extensions;

public static class DependencyInjection
{
    
    
    public static IServiceCollection AddBrowserInstance(this IServiceCollection services, string reportServerUrl)
    {
        services.AddHttpContextAccessor();
        services.Configure<BrowserInstanceConfiguration>(config =>
        {
            config.BaseUrl = reportServerUrl;
        });
        // Add LazyCache, adds InMemoryCache as well
        // We use it to block access to browser instance from multiple threads
        services.AddLazyCache();
        services.AddSingleton<IBrowserInstanceStore, InMemoryBrowserInstanceStore>();
        services.AddSingleton<IBrowserInstanceFactory, PlayWrightBrowserInstanceFactory>();        
        services.AddSingleton<IBrowserInstanceProvider, BrowserInstanceProvider>();
        
        return services;
    }
    public static IServiceCollection AddIngestionAndSemanticSearch(this IServiceCollection services)
    {
        services.AddQdrantCollection<Guid, IngestedChunk>("data-rschatapp-chunks");
        services.AddQdrantCollection<Guid, IngestedDocument>("data-rschatapp-documents");
        services.AddScoped<DataIngestor>();
        services.AddScoped<SemanticSearch.Services.SemanticSearch>();
        services.AddScoped<DocumentLookup>();
        services.AddScoped<SemanticSearchTool>()
            .AddScoped<DocumentLookupTool>();
        
        return services;
    }
    
    
    public static IServiceCollection AddStaticContentServices(this IServiceCollection services, IConfiguration configuration, bool addTools = true)
    {
        services
            .AddOptions<StaticContentOptions>()
            .Bind(configuration.GetSection("StaticContent"))
            .Validate(o => o.Sources is not null, "StaticContent:Sources must not be null")
            .ValidateOnStart();

        services.AddMemoryCache()
            .AddSingleton<IStaticContentIndexStore, StaticContentIndexStore>()
            .AddSingleton<IStaticContentFileStore, StaticContentFileStore>()
            .AddSingleton<IWebRootFileNameProvider, WebRootFileNameProvider>();
        
        if (addTools)
            services.AddScoped<ScriptStoreTool>();
        
        return services;
    }

    
    public static WebApplication UseConfiguredStaticContent(this WebApplication app)
    {
        var staticContentOptions = app.Configuration.GetSection("StaticContent").Get<StaticContentOptions>()
                                  ?? new StaticContentOptions();

        if (staticContentOptions.Sources.Count == 0)
        {
            return app;
        }

        var contentTypeProvider = new FileExtensionContentTypeProvider();
        contentTypeProvider.Mappings[".groovy"] = "text/plain";
        contentTypeProvider.Mappings[".properties"] = "text/plain";
        contentTypeProvider.Mappings[".prefs"] = "text/plain";
        contentTypeProvider.Mappings[".classpath"] = "text/plain";
        contentTypeProvider.Mappings[".project"] = "text/plain";

        foreach (var src in staticContentOptions.Sources)
        {
            if (string.IsNullOrWhiteSpace(src.Name) || string.IsNullOrWhiteSpace(src.Path))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(src.RequestPath))
            {
                continue; // not publicly exposed
            }

            var physicalPath = Path.IsPathRooted(src.Path)
                ? Path.GetFullPath(src.Path)
                : Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, src.Path));

            if (!Directory.Exists(physicalPath))
            {
                app.Logger.LogWarning("StaticContent source {Name} path not found: {Path}", src.Name, physicalPath);
                continue;
            }

            var requestPath = src.RequestPath.StartsWith('/') ? src.RequestPath : "/" + src.RequestPath;
            var maxAge = Math.Max(0, src.CacheMaxAgeSeconds);

            app.Logger.LogInformation(
                "Serving static content source {Name} at {RequestPath} from {PhysicalPath}",
                src.Name,
                requestPath,
                physicalPath);

            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = requestPath,
                FileProvider = new PhysicalFileProvider(physicalPath),
                ContentTypeProvider = contentTypeProvider,
                OnPrepareResponse = ctx =>
                {
                    // Enable client-side caching. Static file middleware still provides ETag/Last-Modified for revalidation.
                    ctx.Context.Response.Headers.CacheControl = $"public,max-age={maxAge}";
                }
            });
        }

        return app;
    }
    
}