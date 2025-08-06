using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RsMcpServer.Identity.Extensions;

namespace TestRsMcpServer.Web;

/// <summary>
/// Mock host environment for testing
/// </summary>
public class MockHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "TestApplication";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

[TestClass]
public sealed class TestAuthentication
{
    private IServiceProvider _serviceProvider = null!;
    
    [TestInitialize]
    public void Initialize()
    {
        var services = new ServiceCollection();
        
        // Add configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Keycloak:Authority", "http://localhost:8080/realms/reportserver"},
                {"Keycloak:ClientId", "reportserver-app"},
                {"Keycloak:ClientSecret", ""},
                {"Keycloak:Realm", "reportserver"},
                {"Keycloak:RequireHttpsMetadata", "false"},
                {"ReportServer:BaseUrl", "http://localhost:8081/reportserver"},
                {"ReportServer:SessionTimeout", "01:00:00"},
                {"ReportServer:EnableSessionBridge", "true"}
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddHttpClient();
        services.AddHttpContextAccessor();
        
        // Create a mock environment for testing
        var environment = new MockHostEnvironment { EnvironmentName = "Development" };
        services.AddSingleton<IHostEnvironment>(environment);
        
        // Add our authentication services
        services.AddKeycloakAuthentication(configuration, environment, setupSessionBridge: true);
        
        _serviceProvider = services.BuildServiceProvider();
    }
    
    
    [TestMethod]
    public void TestConfigurationBinding()
    {
        // Act
        var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
        
        // Assert - Check that authentication configuration is properly bound
        var keycloakAuthority = configuration["Keycloak:Authority"];
        Assert.IsNotNull(keycloakAuthority, "Keycloak:Authority should be configured");
        Assert.AreEqual("http://localhost:8080/realms/reportserver", keycloakAuthority);
        
        var keycloakClientId = configuration["Keycloak:ClientId"];
        Assert.IsNotNull(keycloakClientId, "Keycloak:ClientId should be configured");
        Assert.AreEqual("reportserver-app", keycloakClientId);
        
        var reportServerBaseUrl = configuration["ReportServer:BaseUrl"];
        Assert.IsNotNull(reportServerBaseUrl, "ReportServer:BaseUrl should be configured");
        Assert.AreEqual("http://localhost:8081/reportserver", reportServerBaseUrl);
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

