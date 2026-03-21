using System.Text;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using RSChatApp.Infrastructure.ReportServer.Clients;

namespace RSChatApp.Web.HostedServices.McpTool;

public sealed class RsMcpToolRegistrationHostedService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<Program> _startupLogger;
    private readonly KernelPluginCollection _pluginCollection;

    private IMcpClient? _mcpClient;

    public RsMcpToolRegistrationHostedService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        ILogger<Program> startupLogger,
        KernelPluginCollection pluginCollection)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _startupLogger = startupLogger;
        _pluginCollection = pluginCollection;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var rsUrl = _configuration["RsMcpServer:Url"]
                    ?? throw new InvalidDataException("RsMcpServer:Url");

        _startupLogger.LogInformation("Connecting to RsMcpServer at {Url} to register MCP tools", rsUrl);

        _mcpClient = await McpClientFactory.CreateAsync(
            new SseClientTransport(
                new SseClientTransportOptions
                {
                    Name = RsMcpServerHttpClientName.ClientName,
                    Endpoint = new Uri(rsUrl),
                },
                httpClient: _httpClientFactory.CreateClient(RsMcpServerHttpClientName.ClientName),
                loggerFactory: _loggerFactory),
            cancellationToken: cancellationToken);

        var toolsRs = await _mcpClient.ListToolsAsync(cancellationToken: cancellationToken);

        _startupLogger.LogInformation(
            "Register RsMcpClient toolCalls: {ToolCalls}",
            new StringBuilder().AppendJoin(", ", toolsRs.Select(t => t.Name)));

        _pluginCollection.AddFromFunctions(
            RsMcpServerHttpClientName.ClientName,
            toolsRs.Select(aiFunction => aiFunction.AsKernelFunction()));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_mcpClient is not null)
        {
            await _mcpClient.DisposeAsync();
            _mcpClient = null;
        }
    }
}
