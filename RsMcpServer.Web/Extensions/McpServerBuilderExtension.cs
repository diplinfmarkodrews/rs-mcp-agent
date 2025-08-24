using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;

namespace RsMcpServer.Web.Extensions;

public static class McpServerBuilderExtension
{
    public static IMcpServerBuilder WithTools(this IMcpServerBuilder builder, KernelPluginCollection plugins)
    {
        foreach (var plugin in plugins)
        {
            foreach (var function in plugin)
            {
                builder.Services.AddSingleton(services => McpServerTool.Create(function));
            }
        }

        return builder;
    }
    
    public static IMcpServerBuilder WithToolsFromFactory(this IMcpServerBuilder builder, Func<IServiceProvider, KernelPluginCollection> pluginFactory)
    {
        // Register a service that will provide the tools when needed
        builder.Services.AddSingleton<Func<IServiceProvider, KernelPluginCollection>>(pluginFactory);
        
        // Register tools that will be created on-demand
        builder.Services.AddSingleton<IEnumerable<McpServerTool>>(serviceProvider =>
        {
            var factory = serviceProvider.GetRequiredService<Func<IServiceProvider, KernelPluginCollection>>();
            var plugins = factory(serviceProvider);
            
            var tools = new List<McpServerTool>();
            foreach (var plugin in plugins)
            {
                foreach (var function in plugin)
                {
                    tools.Add(McpServerTool.Create(function));
                }
            }
            return tools;
        });
        
        return builder;
    }
}