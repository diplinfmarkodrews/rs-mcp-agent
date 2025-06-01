using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ReportServerPort;

namespace TestRsMcpServer.Web;

[TestClass]
public sealed class TestRsClient
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    
    [TestInitialize]
    public void Initialize()
    {
        // Create a WebApplicationFactory for the RsMCPServerSDK.Web project
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                // You can override services here if needed
                // builder.ConfigureServices(services => { ... });
            });
        
        // Create an HttpClient to interact with your application
        _client = _factory.CreateClient();
    }
    
    [TestMethod]
    public async Task TestIReportServerClient_Authenticate()
    {
        // Act
        using var scope = _factory.Services.CreateScope();
        var reportServerClient = scope.ServiceProvider.GetRequiredService<IReportServerClient>();
        var response = await reportServerClient.AuthenticateAsync("user", "password");
        // Assert
        Assert.IsNotNull(response);
        if (response.IsSuccess)
        {
            Assert.IsNotNull(response.Data);
            Assert.IsNotNull(response.Data.SessionId);
            Assert.IsNotNull(response.Data.User);
        }
        else
        {
            Assert.IsNotNull(response.Message);
            Assert.IsNotNull(response.Error);
        }
    }
    
    [TestMethod]
    public async Task TestServer_Authentication()
    {
        // Arrange
        var requestData = new { user = "testuser", password = "testpassword" };
        
        // Act
        var response = await _client.PostAsJsonAsync("/rs-authenticate", requestData);
        
        // Assert
        response.EnsureSuccessStatusCode(); // Status code 200-299
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.IsNotNull(result);
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}

