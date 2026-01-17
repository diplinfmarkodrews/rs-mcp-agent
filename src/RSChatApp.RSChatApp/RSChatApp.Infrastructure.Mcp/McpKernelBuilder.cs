// using System.Text;
// using Microsoft.AspNetCore.Builder;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using Microsoft.IdentityModel.Protocols.Configuration;
// using Microsoft.Playwright;
// using Microsoft.SemanticKernel;
// using ModelContextProtocol.Client;
// using RSChatApp.Infrastructure.Mcp.Browser.Tools;
//
// namespace RSChatApp.Infrastructure.Mcp;
//
// public class McpKernelBuilder
// {
//     private readonly IServiceCollection _services;
//
//     public McpKernelBuilder(IServiceCollection services)
//     {
//         _services = services;    
//     }
//
//     public async Task<IServiceCollection> AddRsMcpClientsAsync(string rsMcpUrl)
//     {
//         // Create kernel and register plugins
//         var kernelBuilder = _services.AddKernel();
//
//         // Add BrowserTool plugin
//         kernelBuilder.Plugins.AddFromType<BrowserTool>();
//
//         var scopedServiceProvider = _services.BuildServiceProvider()
//             .CreateScope()
//             .ServiceProvider
//             ;
//         _services.AddSingleton<KernelPluginCollection>(async (s)  =>
//         {
//
//             return new KernelPluginCollection
//             {
//                 
//             };
//         });
//         var startupLogger = scopedServiceProvider.GetRequiredService<ILogger<Program>>();
//         // Creating McpClient with SSE transport
//         await using IMcpClient mcpClientRS = await McpKernelBuilder.CreateAsync(
//             new SseClientTransport(
//                 new SseClientTransportOptions
//                 {
//                     Name = "RsMcpServer",
//                     Endpoint = new Uri(rsMcpUrl 
//                                        ?? throw new InvalidConfigurationException("RsMcpServer:Url")),
//                 },
//                 httpClient: scopedServiceProvider
//                     .GetRequiredService<IHttpClientFactory>()
//                     .CreateClient("RsMcpServer"),
//                 loggerFactory: scopedServiceProvider
//                     .GetRequiredService<ILoggerFactory>()
//             ));
//         var toolsRs = await mcpClientRS.ListToolsAsync();
//         startupLogger.LogInformation("Register RsMcpClient with toolCalls: {toolCalls}", 
//             new StringBuilder().AppendJoin(", ", toolsRs.Select(t => t.Name)));
//
//         #pragma warning disable SKEXP0001
//         //Add the RsMcpServer tools to kernel builder for static registration
//         kernelBuilder.Plugins.AddFromFunctions("RsMcpServer", 
//             toolsRs.Select(aiFunction => aiFunction.AsKernelFunction()));
//         #pragma warning restore SKEXP0001
//         foreach (var clientConfig in mcpClientSettings.Clients ?? Enumerable.Empty<McpClientConfiguration>())
//         {
//             // Create an MCPClient for each configured client
//             await using IMcpClient mcpClient = await McpKernelBuilder.CreateAsync(new StdioClientTransport(new()
//             {
//                 Name = clientConfig.Name,
//                 Command = clientConfig.Command,
//                 Arguments = clientConfig.Arguments?.ToArray() ?? Array.Empty<string>(),
//             }));
//             var tools = await mcpClient.ListToolsAsync();
//             startupLogger.LogInformation("Register McpClient: {clientConfigName} with toolCalls: {toolCalls}", clientConfig.Name, 
//                 new StringBuilder().AppendJoin(", ", tools.Select(t => t.Name)));
//         #pragma warning disable SKEXP0001
//             kernelBuilder.Plugins.AddFromFunctions(clientConfig.Name, 
//                 tools.Select(aiFunction => aiFunction.AsKernelFunction()));
//         #pragma warning restore SKEXP0001
//         }
//         return _services;
//     }
// }