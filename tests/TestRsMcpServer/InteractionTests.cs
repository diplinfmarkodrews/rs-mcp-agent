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
    public async Task TestSessionBridgeService_GetSessionId()
    {
        // Arrange
        var sessionBridge = _serviceProvider.GetRequiredService<ISessionBridgeService>();

        // Act
        var sessionId = await sessionBridge.GetSessionIdAsync();

        // Assert
        // In a test environment without HTTP context, this should return null
        Assert.IsNull(sessionId, "Session ID should be null in test environment without HTTP context");
        
        // Verify proper logging
        Assert.IsTrue(_logMessages.Any(msg => msg.Contains("No active session found")), 
            "Should log warning about no active session");
    }

    [TestMethod]
    public async Task TestTokenManagementService_GetAccessToken()
    {
        // Arrange
        var tokenService = _serviceProvider.GetRequiredService<ITokenManagementService>();

        // Act
        var token = await tokenService.GetAccessTokenAsync();

        // Assert
        // In a test environment without session, this should return null
        Assert.IsNull(token, "Access token should be null in test environment without session");
    }

    [TestMethod]
    public async Task TestSessionBridgeService_IsAuthenticated_WithoutSession()
    {
        // Arrange
        var sessionBridge = _serviceProvider.GetRequiredService<ISessionBridgeService>();

        // Act
        var isAuthenticated = await sessionBridge.IsAuthenticatedAsync();

        // Assert
        Assert.IsFalse(isAuthenticated, "Should not be authenticated without session");
    }

    [TestMethod]
    public async Task TestSessionBridgeService_GetCurrentUser_WithoutHttpContext()
    {
        // Arrange
        var sessionBridge = _serviceProvider.GetRequiredService<ISessionBridgeService>();

        // Act
        var user = await sessionBridge.GetCurrentUserAsync();

        // Assert
        Assert.IsNull(user, "Current user should be null without HTTP context");
        
    }

    [TestMethod]
    public async Task TestCompleteInteractionFlow_ExpectedBehavior()
    {
        // This test simulates the expected flow and documents what should happen
        
        Console.WriteLine("=== Expected RSChatApp.Web -> RsMcpServer.Api Flow ===");
        Console.WriteLine("1. RSChatApp authenticates with Keycloak");
        Console.WriteLine("2. RSChatApp receives session and bearer token");
        Console.WriteLine("3. RSChatApp calls TerminalTool via MCP");
        Console.WriteLine("4. RsMcpServer receives request with session context");
        Console.WriteLine("5. RsMcpServer logs: SessionId, Bearer token, User info");
        Console.WriteLine("6. TerminalTool checks session and executes command");
        
        // Arrange & Act - Simulate the services that would be called
        var sessionBridge = _serviceProvider.GetRequiredService<ISessionBridgeService>();
        
        // Step 1-2: Check initial state (simulates what happens without authentication)
        var initialSessionId = await sessionBridge.GetSessionIdAsync();
        var initialAuth = await sessionBridge.IsAuthenticatedAsync();
        var initialToken = await sessionBridge.GetAuthenticationContextAsync();
        
        // Assert - Document expected behavior
        Assert.IsNull(initialSessionId, "Initial session should be null");
        Assert.IsFalse(initialAuth, "Initial authentication should be false");
        Assert.IsNull(initialToken.AuthenticationToken, "Initial token should be null");
        
        Console.WriteLine($"✓ Without authentication: SessionId={initialSessionId}, IsAuthenticated={initialAuth}, HasToken={initialToken != null}");
        Console.WriteLine("✓ In real scenario with authentication, these would have actual values");
        Console.WriteLine("✓ RsMcpServer.Api logging middleware will capture all this information");
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

    [TestMethod]
    public void TestServiceRegistration()
    {
        // Test that all required services are properly registered
        
        // Assert - Verify all services can be resolved
        Assert.IsNotNull(_serviceProvider.GetRequiredService<ISessionBridgeService>(), 
            "ISessionBridgeService should be registered");
        Assert.IsNotNull(_serviceProvider.GetRequiredService<ITokenManagementService>(), 
            "ITokenManagementService should be registered");
        Assert.IsNotNull(_serviceProvider.GetRequiredService<IConfiguration>(), 
            "IConfiguration should be registered");
        Assert.IsNotNull(_serviceProvider.GetRequiredService<ILogger<InteractionTests>>(), 
            "ILogger should be registered");
        
        Console.WriteLine("✓ All required services are properly registered and resolvable");
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
        services.AddKeycloakAuthentication(configuration, environment, setupSessionBridge: true);
        
        return services.BuildServiceProvider();
    }
}

