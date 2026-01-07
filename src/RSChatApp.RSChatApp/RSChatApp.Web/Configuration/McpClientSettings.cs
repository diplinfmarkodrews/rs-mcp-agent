public class McpClientSettings
{
    public IEnumerable<McpClientConfiguration> Clients { get; set; }
}

public class McpClientConfiguration
{
    public string Name { get; set; }
    public string Command { get; set; }
    public IEnumerable<string> Arguments { get; set; }
}
