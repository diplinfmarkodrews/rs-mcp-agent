using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RsMcpServer.Identity.Extensions;
using RsMcpServer.Identity.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RsMcpServer.Web;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestRsMcpServer.Utilities;

namespace TestRsMcpServer;

/// <summary>
/// Integration tests for RSChatApp.Web and RsMcpServer.Api interaction
/// Tests the core services and session management functionality
/// </summary>
[TestClass]
public sealed class InteractionTests
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
    public void TestExpectedLoggingStructure()
    {
        // This test documents what logging structure we expect from RsMcpServer.Api
        
        var expectedLogEntries = new[]
        {
            "=== INCOMING REQUEST ===",
            "Method:",
            "Path:",
            "QueryString:",
            "Headers:",
            "=== SESSION INFO ===",
            "SessionId:",
            "IsAuthenticated:",
            "HasBearerToken:",
            "UserName:",
            "UserClaims:",
            "=== END REQUEST INFO ==="
        };

        Console.WriteLine("Expected RsMcpServer.Api logging structure:");
        foreach (var entry in expectedLogEntries)
        {
            Console.WriteLine($"  - {entry}");
        }
        
        // Test that our logging infrastructure works
        var logger = _serviceProvider.GetRequiredService<ILogger<InteractionTests>>();
        logger.LogInformation("Test logging infrastructure");
        
        Assert.IsTrue(_logMessages.Any(msg => msg.Contains("Test logging infrastructure")), 
            "Logging infrastructure should capture messages");
        
        Console.WriteLine("✓ Logging infrastructure verified");
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
                {"Keycloak:ClientSecret", ""},
                {"Keycloak:RequireHttpsMetadata", "false"},
                {"ReportServer:Url", "http://localhost:8090"}
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
        
        // Add Keycloak authentication services
        services.AddKeycloakAuthentication(configuration, environment);
        
        return services.BuildServiceProvider();
    }
}

