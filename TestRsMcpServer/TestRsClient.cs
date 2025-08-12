using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RsMcpServer.Identity.Extensions;

namespace TestRsMcpServer;

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
        
        // Add Keycloak authentication with session bridge
        services.AddKeycloakAuthentication(configuration, environment, setupSessionBridge: true);
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [TestMethod]
    public void TestKeycloakConfiguration()
    {
        var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
        
        Assert.AreEqual("http://localhost:8080/realms/reportserver", 
            configuration["Keycloak:Authority"]);
        Assert.AreEqual("reportserver-app", 
            configuration["Keycloak:ClientId"]);
    }
}
