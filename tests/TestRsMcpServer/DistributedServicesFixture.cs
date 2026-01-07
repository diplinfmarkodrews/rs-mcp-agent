using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace TestRsMcpServer;

[CollectionDefinition("Distributed Services")]
public class DistributedServicesCollection : ICollectionFixture<DistributedServicesFixture>
{
}

public class DistributedServicesFixture : IAsyncLifetime
{
    private readonly int _rsMcpServerPort;
    private readonly int _rsChatAppPort;
    
    public WebApplicationFactory<RsMcpServer.Web.Program> RsMcpServerFactory { get; private set; }
    public WebApplicationFactory<RSChatApp.Web.Program> RsChatAppFactory { get; private set; }
    public HttpClient RsMcpServerClient { get; private set; }
    public HttpClient RsChatAppClient { get; private set; }
    
    public string RsMcpServerBaseUrl => $"http://localhost:{_rsMcpServerPort}";
    public string RsChatAppBaseUrl => $"http://localhost:{_rsChatAppPort}";

    public DistributedServicesFixture()
    {
        _rsMcpServerPort = PortHelper.GetAvailablePort();
        _rsChatAppPort = PortHelper.GetAvailablePort();
    }

    public async Task InitializeAsync()
    {
        // Start Service2 first (dependency)
        RsMcpServerFactory = new WebApplicationFactory<RsMcpServer.Web.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseUrls(RsMcpServerBaseUrl);
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    
                });
            });

        RsMcpServerClient = RsMcpServerFactory.CreateClient();

        // Start RsChatApp with RsMcpServer dependency configured
        RsChatAppFactory = new WebApplicationFactory<RSChatApp.Web.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseUrls(RsChatAppBaseUrl);
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["RsMcpServer:Url"] = RsMcpServerBaseUrl,
                    });
                });
                builder.ConfigureServices(services =>
                {
                    
                });
            });

        RsChatAppClient = RsChatAppFactory.CreateClient();

        // Wait for both services to be ready
        await Task.WhenAll(
            WaitForServiceReady(RsMcpServerClient, "/health"),
            WaitForServiceReady(RsChatAppClient, "/health")
        );
    }

    public Task DisposeAsync()
    {
        RsMcpServerClient?.Dispose();
        RsChatAppClient?.Dispose();
        RsMcpServerFactory?.Dispose();
        RsChatAppFactory?.Dispose();
        return Task.CompletedTask;
    }

    private static async Task WaitForServiceReady(HttpClient client, string healthEndpoint, int maxAttempts = 10)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                var response = await client.GetAsync(healthEndpoint);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch
            {
                // Service not ready yet
            }
            
            await Task.Delay(500);
        }
        
        throw new InvalidOperationException($"Service did not become ready within expected time");
    }
}

public static class PortHelper
{
    public static int GetAvailablePort()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.LocalEndPoint).Port;
    }
}