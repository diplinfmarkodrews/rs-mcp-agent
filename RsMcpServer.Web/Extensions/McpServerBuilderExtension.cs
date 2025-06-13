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
                builder.Services.AddSingleton(services => McpServerTool.Create(function.AsAIFunction()));
            }
        }

        return builder;
    }
}