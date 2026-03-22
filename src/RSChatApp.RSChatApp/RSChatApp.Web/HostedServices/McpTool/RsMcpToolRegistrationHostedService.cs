using System.Text;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using RSChatApp.Infrastructure.ReportServer.Clients;
using RSChatApp.Web.Configuration;

namespace RSChatApp.Web.HostedServices.McpTool;

public sealed class RsMcpToolRegistrationHostedService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<Program> _startupLogger;
    private readonly KernelPluginCollection _pluginCollection;
    private readonly McpClientSettings _mcpClientSettings;

    private readonly List<McpClient> _mcpClients = [];

    public RsMcpToolRegistrationHostedService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        McpClientSettings mcpClientSettings,
        ILogger<Program> startupLogger,
        KernelPluginCollection pluginCollection)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _startupLogger = startupLogger;
        _pluginCollection = pluginCollection;
        _mcpClientSettings = mcpClientSettings;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var rsUrl = _configuration["RsMcpServer:Url"]
                    ?? throw new InvalidDataException("RsMcpServer:Url");

        _startupLogger.LogInformation("Connecting to RsMcpServer at {Url} to register MCP tools", rsUrl);

        var httpClient = await McpClient.CreateAsync(
            new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Name = RsMcpServerHttpClientName.ClientName,
                    Endpoint = new Uri(rsUrl),
                },
                httpClient: _httpClientFactory.CreateClient(RsMcpServerHttpClientName.ClientName),
                loggerFactory: _loggerFactory),
            cancellationToken: cancellationToken);
        _mcpClients.Add(httpClient);

        var toolsRs = await httpClient.ListToolsAsync(cancellationToken: cancellationToken);

        _startupLogger.LogInformation(
            "Register RsMcpClient toolCalls: {ToolCalls}",
            new StringBuilder().AppendJoin(", ", toolsRs.Select(t => t.Name)));

        _pluginCollection.AddFromFunctions(
            RsMcpServerHttpClientName.ClientName,
            toolsRs.Select(t => t.AsKernelFunction()));

        if (_mcpClientSettings.Clients is null)
            return;

        foreach (var clientConfig in _mcpClientSettings.Clients)
        {
            _startupLogger.LogInformation(
                "Registering MCP tools from client {ClientName} with command {Command} and arguments {Arguments}",
                clientConfig.Name, clientConfig.Command,
                string.Join(' ', clientConfig.Arguments ?? Array.Empty<string>()));

            var stdioClient = await McpClient.CreateAsync(
                new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = clientConfig.Name,
                    Command = clientConfig.Command,
                    Arguments = clientConfig.Arguments?.ToList(),
                }),
                cancellationToken: cancellationToken);
            _mcpClients.Add(stdioClient);

            var tools = await stdioClient.ListToolsAsync(cancellationToken: cancellationToken);

            _pluginCollection.AddFromFunctions(
                clientConfig.Name,
                tools.Select(t => t.AsKernelFunction()));
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var client in _mcpClients)
            await client.DisposeAsync();
        _mcpClients.Clear();
    }
}
