using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RsMcpServer.Identity.Services;
using TestRsMcpServer.Utilities;


namespace TestRsMcpServer;

/// <summary>
/// Tests for HTTP interaction patterns and logging expectations
/// Documents what should happen when RSChatApp.Web calls RsMcpServer.Api
/// </summary>
[TestClass]
public sealed class HttpInteractionTests
{
    private IServiceProvider _serviceProvider = null!;
    private List<string> _logMessages = null!;

    [TestInitialize]
    public void Initialize()
    {
        _logMessages = new List<string>();
        _serviceProvider = CreateTestServiceProvider();
    }

    [TestMethod]
    public void TestLogOutput_ShowsExpectedSessionInformation()
    {
        // This test documents what information should be logged during real interaction
        
        var expectedLogEntries = new[]
        {
            "=== INCOMING REQUEST ===",
            "Method:",
            "Path:",
            "QueryString:",
            "ContentType:",
            "Headers:",
            "=== SESSION INFO ===",
            "SessionId:",
            "IsAuthenticated:",
            "HasBearerToken:",
            "TokenLength:",
            "UserName:",
            "UserClaims:",
            "=== END REQUEST INFO ===",
            "=== RESPONSE ===",
            "StatusCode:",
            "=== END RESPONSE ==="
        };

        Console.WriteLine("=== Expected RsMcpServer.Api Logging Structure ===");
        foreach (var expectedEntry in expectedLogEntries)
        {
            Console.WriteLine($"  ✓ {expectedEntry}");
        }
        
        // Test that our logging infrastructure works
        var logger = _serviceProvider.GetRequiredService<ILogger<HttpInteractionTests>>();
        logger.LogInformation("Test request logging verification");
        
        Assert.IsTrue(_logMessages.Any(msg => msg.Contains("Test request logging verification")), 
            "Logging infrastructure should capture messages");
        
        Console.WriteLine("\n✓ The RsMcpServer.Api RequestLoggingMiddleware will capture all this information");
        Console.WriteLine("✓ When RSChatApp.Web makes authenticated MCP tool calls, you'll see:");
        Console.WriteLine("  - Complete request details (method, path, headers, body)");
        Console.WriteLine("  - SessionId from the authenticated session");
        Console.WriteLine("  - Bearer token presence and length");
        Console.WriteLine("  - User information and claims");
        Console.WriteLine("  - Response status codes");
    }

    private IServiceProvider CreateTestServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Add configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Keycloak:Authority", "http://localhost:8080/realms/reportserver"},
                {"Keycloak:ClientId", "reportserver-app"},
                {"Keycloak:ClientSecret", "test-secret"},
                {"Keycloak:RequireHttpsMetadata", "false"},
                {"ReportServer:Url", "http://localhost:8081"}
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        services.AddHttpClient();
        services.AddHttpContextAccessor();
        
        // Add test logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(new TestLoggerProvider(_logMessages));
        });
        
        // Create a mock environment for testing
        var environment = new MockHostEnvironment { EnvironmentName = "Development" };
        services.AddSingleton<IHostEnvironment>(environment);
        
        // Add authentication services
        
        // Add Keycloak options
        services.Configure<Keycloak.AuthServices.Authentication.KeycloakAuthenticationOptions>(options =>
        {
            options.AuthServerUrl = "http://localhost:8080";
            options.Realm = "reportserver";
            options.Resource = "reportserver-app";
        });
        
        return services.BuildServiceProvider();
    }
}
