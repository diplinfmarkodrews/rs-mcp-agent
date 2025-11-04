using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RsMcpServer.Identity.Services;
using Microsoft.Extensions.Logging;
using ReportServer.Abstraction;
using ReportServer.Abstraction.Contracts;
using ReportServer.Abstraction.Contracts.Terminal;
using ReportServer.Abstraction.Contracts.Authentication; // Correct namespace
using Microsoft.Extensions.FileProviders;
using RSChatApp.Mcp.ReportServer.Tools;

namespace TestRsMcpServer;

/// <summary>
/// Dedicated tests for TerminalTool interaction and session management
/// </summary>
[TestClass]
public sealed class TerminalToolInteractionTests
{
    private IServiceProvider _serviceProvider = null!;
    private ILogger<TerminalToolInteractionTests> _logger = null!;
    private List<string> _logMessages = null!;

    [TestInitialize]
    public void Initialize()
    {
        _logMessages = new List<string>();
        _serviceProvider = CreateTestServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<TerminalToolInteractionTests>>();
    }

    [TestMethod]
    public async Task TestTerminalTool_NoSession_ReturnsAuthenticationError()
    {
        // Arrange
        
        var reportServerClient = _serviceProvider.GetRequiredService<IReportServerClient>();
        var terminalLogger = _serviceProvider.GetRequiredService<ILogger<TerminalTool>>();
        var sessionBridge = _serviceProvider.GetRequiredService<ISessionBridgeService>();
        var terminalTool = new TerminalTool(terminalLogger, sessionBridge, reportServerClient);
        var terminalSession = await reportServerClient.InitSessionAsync();
        // Act
        Assert.IsNotNull(terminalSession.Data?.SessionId);
        var result = await terminalTool.ExecuteCommandAsync(terminalSession.Data?.SessionId, "ls -la");

        // Assert
        Assert.IsNotNull(result, "Result should not be null");
        Assert.IsTrue(result.Contains("Authentication required"), 
            "Should return authentication error when no session is available");
        
        // Verify the log messages
        Assert.IsTrue(_logMessages.Any(msg => msg.Contains("Executing terminal command: ls -la")), 
            "Should log the command being executed");
    }

    [TestMethod]
    public async Task TestSessionBridgeService_GetSessionId_WithoutHttpContext()
    {
        // Arrange
        var sessionBridge = _serviceProvider.GetRequiredService<ISessionBridgeService>();

        // Act
        var sessionId = await sessionBridge.GetSessionIdAsync();

        // Assert
        Assert.IsNull(sessionId, "Session ID should be null without HTTP context");
        
        // Verify that the appropriate warning was logged
        Assert.IsTrue(_logMessages.Any(msg => msg.Contains("No active session found")), 
            "Should log warning about no active session");
    }

    [TestMethod]
    public async Task TestSessionBridgeService_GetBearerToken_WithoutSession()
    {
        // Arrange
        var sessionBridge = _serviceProvider.GetRequiredService<ISessionBridgeService>();

        // Act
        var bearerToken = await sessionBridge.GetAuthenticationTokenAsync();

        // Assert
        Assert.IsNull(bearerToken, "Bearer token should be null without session");
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
    public async Task TestTokenManagementService_RefreshToken_WithoutRefreshToken()
    {
        // Arrange
        var tokenService = _serviceProvider.GetRequiredService<ITokenManagementService>();

        // Act
        var refreshResult = await tokenService.RefreshTokenAsync();

        // Assert
        Assert.IsFalse(refreshResult.Success, "Refresh should fail without refresh token");
        Assert.AreEqual("No refresh token available", refreshResult.Message);
    }

    [TestMethod]
    public async Task TestCompleteFlow_SessionCreation_And_TerminalExecution()
    {
        // This test simulates the complete flow from session creation to terminal execution
        
        // Arrange
        var sessionBridge = _serviceProvider.GetRequiredService<ISessionBridgeService>();
        var reportServerClient = _serviceProvider.GetRequiredService<IReportServerClient>();
        var terminalLogger = _serviceProvider.GetRequiredService<ILogger<TerminalTool>>();
        
        // Fix: Correct parameter order - logger, reportServerClient, sessionBridge
        var terminalTool = new TerminalTool(terminalLogger, sessionBridge, reportServerClient);

        // Act & Assert - Step 1: Verify no initial session
        var initialSessionId = await sessionBridge.GetSessionIdAsync();
        Assert.IsNull(initialSessionId, "Should have no initial session");

        // Step 2: Verify authentication status
        var isAuthenticated = await sessionBridge.IsAuthenticatedAsync();
        Assert.IsFalse(isAuthenticated, "Should not be initially authenticated");
        var terminalSession = await reportServerClient.InitSessionAsync();
        Assert.IsNull(terminalSession.Data?.SessionId, "Session should not be initialized");
        // Step 3: Attempt terminal command execution
        var terminalResult = await terminalTool.ExecuteCommandAsync(terminalSession.Data?.SessionId, "echo 'test'");
        
        // Step 4: Verify authentication error
        Assert.IsTrue(terminalResult.Contains("Authentication required"), 
            "Terminal execution should require authentication");

        // Step 5: Verify appropriate logging occurred
        var loggedMessages = string.Join("\n", _logMessages);
        Assert.IsTrue(loggedMessages.Contains("Executing terminal command"), 
            "Should log terminal command execution attempt");
    }

    [TestMethod]
    public void TestLoggedMessages_ContainExpectedInformation()
    {
        // This test verifies that our logging infrastructure is working correctly
        
        // Act
        _logger.LogInformation("Test message for verification");
        
        // Assert
        Assert.IsTrue(_logMessages.Any(msg => msg.Contains("Test message for verification")), 
            "Test logger should capture log messages");
    }

    private IServiceProvider CreateTestServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Add configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Keycloak:Authority", "http://localhost:8080/realms/reportserver"},
                {"Keycloak:ClientId", "rs-chat-app"},
                {"Keycloak:ClientSecret", "test-secret"},
                {"Keycloak:RequireHttpsMetadata", "false"},
                {"ReportServer:Url", "http://localhost:8080/reportserver"}
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
        
        // Add ReportServer client (mock)
        services.AddScoped<IReportServerClient, MockReportServerClient>();
        
        // Add authentication services
        services.AddScoped<ITokenManagementService, TokenManagementService>();
        services.AddScoped<ISessionBridgeService, SessionBridgeService>();
        
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

/// <summary>
/// Test logger provider for capturing log messages during tests
/// </summary>
public class TestLoggerProvider : ILoggerProvider
{
    private readonly List<string> _logMessages;

    public TestLoggerProvider(List<string> logMessages)
    {
        _logMessages = logMessages;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(_logMessages);
    }

    public void Dispose() { }
}

/// <summary>
/// Test logger implementation for capturing log messages
/// </summary>
public class TestLogger : ILogger
{
    private readonly List<string> _logMessages;

    public TestLogger(List<string> logMessages)
    {
        _logMessages = logMessages;
    }

    public IDisposable? BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logMessages.Add(formatter(state, exception));
    }
}

/// <summary>
/// Simplified mock implementation of IReportServerClient for testing
/// </summary>
public class MockReportServerClient : IReportServerClient
{
    // IRsAuthenticationClient implementation
    public Task<Result<AuthenticationResult>> AuthenticateAsync(string username, string password)
    {
        return Task.FromResult(new Result<AuthenticationResult>(
            new Exception("Mock: Authentication not implemented")));
    }

    public Task<Result<string>> LogoutAsync()
    {
        return Task.FromResult(new Result<string>(
            new Exception("Mock: Logout not implemented")));
    }

    // IRsTerminalClient implementation  
    public Task<Result> CloseSessionAsync(string sessionId)
    {
        // Fix: Use the correct Result factory method
        return Task.FromResult(Result.Fail("Mock: Close session not implemented"));
    }

    public Task<Result<TerminalSessionInfo>> InitSessionAsync(AbstractNode? node = null, Dictionary<string, string>? mapper = null)
    {
        return Task.FromResult(new Result<TerminalSessionInfo>(
            new Exception("Mock: Session init not implemented")));
    }

    public Task<Result<CommandResult>> ExecuteAsync(string sessionId, string command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Result<CommandResult>(
            new Exception("Mock: Command execution not implemented")));
    }

    public Task<Result<CommandResult>> CtrlCPressedAsync(string sessionId)
    {
        return Task.FromResult(new Result<CommandResult>(
            new Exception("Mock: Ctrl+C not implemented")));
    }

    public void Dispose() { }
}
