namespace RSChatApp.Web.Configuration;

public class McpClientSettings
{
    public IEnumerable<McpClientConfiguration>? Clients { get; set; }
}

public class McpClientConfiguration
{
    public required string Name { get; set; }
    public required string Command { get; set; }
    public IEnumerable<string>? Arguments { get; set; }
}