
using ModelContextProtocol.Client;

namespace RSChatApp.Web.Configuration;

public class McpClientSettings
{
    public IEnumerable<McpClientConfiguration>? Clients { get; set; }
    internal async Task<List<(string Name, IList<McpClientTool>)>> CreateMcpClientsFromConfigAsync()
    {
        var configuredClientTools = new List<(string Name, IList<McpClientTool> Tools)>();
    
        if (Clients is not null)
        {
            foreach (var clientConfig in Clients)
            {
                var mcpClient = await McpClient.CreateAsync(
                    new StdioClientTransport(new StdioClientTransportOptions
                    {
                        Name = clientConfig.Name,
                        Command = clientConfig.Command,
                        Arguments = clientConfig.Arguments?.ToList(),
                    }));
        
                configuredClientTools.Add((clientConfig.Name, await mcpClient.ListToolsAsync()));
            }
        }
        return configuredClientTools;
    }
}

public class McpClientConfiguration
{
    public required string Name { get; set; }
    public required string Command { get; set; }
    public IEnumerable<string>? Arguments { get; set; }
}