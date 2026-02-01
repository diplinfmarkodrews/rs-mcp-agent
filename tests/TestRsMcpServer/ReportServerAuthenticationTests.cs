using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReportServer.Abstraction;
using ReportServer.RpcClient.Extensions;

namespace TestRsMcpServer;

/// <summary>
/// Integration tests for ReportServerClient authentication against a real ReportServer instance
/// These tests validate the authentication flow between RsMcpServer and ReportServer
/// </summary>
[TestClass]
[TestCategory("Integration")]
public sealed class ReportServerAuthenticationTests
{
    private IReportServerClient _reportServerClient = null!;
    private IServiceProvider _serviceProvider = null!;
    private readonly string _testUsername = "root"; // Replace with a valid test user in your ReportServer instance
    private readonly string _testPassword = "root"; // Replace with the correct password
    private readonly string _reportServerUrl = "http://localhost:8080/reportserver"; // Replace with your ReportServer URL
    private readonly List<string> _logMessages = new();

    [TestInitialize]
    public void Initialize()
    {
        // Create services with the real ReportServerGwtRpcClient
        var services = new ServiceCollection();
        
        // Add configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"ReportServer:Url", _reportServerUrl }
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(new TestLoggerProvider(_logMessages));
        });
        
        // Register the real ReportServerClient implementation
        services.AddReportServerRpcClient(_reportServerUrl);
        
        // Build service provider
        _serviceProvider = services.BuildServiceProvider();
        
        // Get ReportServerClient
        _reportServerClient = _serviceProvider.GetRequiredService<IReportServerClient>();
    }

    [TestMethod]
    [TestCategory("Authentication")]
    public async Task ReportServerClient_DirectAuthentication_Success()
    {
        // This test only works if ReportServer is actually running and accessible
        // Skip if ReportServer is not available
        if (!await IsReportServerAvailable())
        {
            Assert.Inconclusive("ReportServer is not available. Skipping test.");
            return;
        }
        
        // Arrange
        Assert.IsNotNull(_reportServerClient, "ReportServerClient should not be null");

        // Act
        var authResult = await _reportServerClient.AuthenticateAsync(_testUsername, _testPassword);

        // Assert
        Assert.IsNotNull(authResult, "Authentication result should not be null");
        
        // Log the result
        Console.WriteLine($"Authentication result: {(authResult.IsSuccess ? "Success" : "Failed")}");
        Console.WriteLine($"Message: {authResult.Message}");
        
        Assert.IsTrue(authResult.IsSuccess);       
        Assert.IsNotNull(authResult.Data, "Authentication result data should not be null");
        Assert.IsTrue(authResult.Data.IsAuthenticated, "User should be authenticated");
        Assert.IsNotNull(authResult.Data.User, "User information should be included");
        Assert.AreEqual(_testUsername, authResult.Data.User.Username, "Username should match");
        Assert.IsFalse(string.IsNullOrEmpty(authResult.Data.SessionId), "Session ID should be provided");
        
        Console.WriteLine($"✓ Successfully authenticated as {authResult.Data.User.Username}");
        Console.WriteLine($"✓ Session ID: {authResult.Data.SessionId}");
        Console.WriteLine($"✓ User details: {authResult.Data.User.Firstname} {authResult.Data.User.Lastname} ({authResult.Data.User.Email})");

    }

    [TestMethod]
    [TestCategory("Authentication")]
    public async Task ReportServerClient_DirectAuthentication_Failure()
    {
        // This test only works if ReportServer is actually running and accessible
        // Skip if ReportServer is not available
        if (!await IsReportServerAvailable())
        {
            Assert.Inconclusive("ReportServer is not available. Skipping test.");
            return;
        }
        
        // Arrange - Use invalid credentials
        var invalidUsername = "invalid_user";
        var invalidPassword = "invalid_password";

        // Act
        var authResult = await _reportServerClient.AuthenticateAsync(invalidUsername, invalidPassword);

        // Assert
        Assert.IsNotNull(authResult, "Authentication result should not be null");
        Assert.IsFalse(authResult.IsSuccess, "Authentication should fail with invalid credentials");
        Assert.IsTrue(!string.IsNullOrEmpty(authResult.Message), "Error message should be provided");
        
        Console.WriteLine($"✓ Authentication correctly failed with message: {authResult.Message}");
    }

    [TestMethod]
    [TestCategory("Authentication")]
    public async Task ReportServerClient_GetHmacPassphrase_Success()
    {
        // This test only works if ReportServer is actually running and accessible
        // Skip if ReportServer is not available
        if (!await IsReportServerAvailable())
        {
            Assert.Inconclusive("ReportServer is not available. Skipping test.");
            return;
        }
        
        // Arrange - Cast to concrete implementation and use reflection to access private authentication client
        var gwtRpcClient = _reportServerClient as ReportServer.RpcClient.Services.ReportServerGwtRpcClient;
        Assert.IsNotNull(gwtRpcClient, "ReportServerClient should be castable to ReportServerGwtRpcClient");

        // Use reflection to access the private _authenticationClient field
        var authClientField = gwtRpcClient.GetType().GetField("_authenticationClient", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(authClientField, "Authentication client field should exist");
        
        var authClient = authClientField.GetValue(gwtRpcClient) as ReportServer.RpcClient.Services.RsGwtRpcAuthenticationClient;
        Assert.IsNotNull(authClient, "Authentication client should not be null");

        // Act
        var hmacResult = await authClient.GetHmacPassphraseAsync();

        // Assert
        Assert.IsNotNull(hmacResult, "HMAC passphrase result should not be null");
        
        Console.WriteLine($"HMAC passphrase result: {(hmacResult.Success ? "Success" : "Failed")}");
        Console.WriteLine($"Message: {hmacResult.Result}");
        
        if (hmacResult.Success)
        {
            Assert.IsNotNull(hmacResult.Result, "HMAC passphrase should not be null");
            Assert.IsFalse(string.IsNullOrEmpty(hmacResult.Result), "HMAC passphrase should not be empty");
            
            Console.WriteLine($"✓ Successfully retrieved HMAC passphrase");
            Console.WriteLine($"✓ Passphrase length: {hmacResult.Result.Length} characters");
            Console.WriteLine($"✓ Passphrase preview: {hmacResult.Result.Substring(0, Math.Min(20, hmacResult.Result.Length))}...");
            
            // Verify it contains expected passphrase text
            Assert.IsTrue(hmacResult.Result.Contains("Passphrase"), "HMAC passphrase should contain 'Passphrase' text");
        }
        else
        {
            Assert.Fail($"Failed to retrieve HMAC passphrase: {hmacResult.Error}");
        }
    }

    [TestMethod]
    [TestCategory("Terminal")]
    public async Task ReportServerClient_TerminalSession_AfterAuthentication()
    {
        // This test only works if ReportServer is actually running and accessible
        // Skip if ReportServer is not available
        if (!await IsReportServerAvailable())
        {
            Assert.Inconclusive("ReportServer is not available. Skipping test.");
            return;
        }
        
        // Step 1: Authenticate
        var authResult = await _reportServerClient.AuthenticateAsync(_testUsername, _testPassword);
        
        // Skip the rest of the test if authentication fails
        if (!authResult.IsSuccess)
        {
            Assert.Inconclusive($"Authentication failed: {authResult.Message}. Skipping terminal tests.");
            return;
        }
        
        // Step 2: Initialize terminal session
        var terminalResult = await _reportServerClient.InitSessionAsync();
        Assert.IsTrue(terminalResult.IsSuccess, $"Terminal session initialization should succeed. Error: {terminalResult.Message}");
        Assert.IsNotNull(terminalResult.Data, "Terminal session data should not be null");
        Assert.IsFalse(string.IsNullOrEmpty(terminalResult.Data.SessionId), "Terminal session ID should be provided");
        
        Console.WriteLine($"✓ Terminal session created with ID: {terminalResult.Data.SessionId}");
        
        // Step 3: Execute a command
        var command = "echo 'Hello from ReportServer terminal'";
        var commandResult = await _reportServerClient.ExecuteAsync(terminalResult.Data.SessionId, command);
        
        // Assert command execution
        Assert.IsTrue(commandResult.IsSuccess, $"Command execution should succeed. Error: {commandResult.Message}");
        Assert.IsNotNull(commandResult.Data, "Command result should not be null");
        
        if (commandResult.Data.Result != null)
        {
            Console.WriteLine($"✓ Command output: {commandResult.Data.Result}");
        }
        
        // Clean up - Close the terminal session
        var closeResult = await _reportServerClient.CloseSessionAsync(terminalResult.Data.SessionId);
        Assert.IsTrue(closeResult.IsSuccess, "Terminal session should close successfully");
    }

    /// <summary>
    /// Checks if ReportServer is available before running integration tests
    /// </summary>
    private async Task<bool> IsReportServerAvailable()
    {
        try
        {
            using var httpClient = new HttpClient();
            
            // Try to reach ReportServer's base URL
            var response = await httpClient.GetAsync(_reportServerUrl);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✓ ReportServer is available for integration testing");
                return true;
            }
            else
            {
                Console.WriteLine($"⚠️ ReportServer returned status code: {response.StatusCode}");
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"⚠️ Cannot connect to ReportServer: {ex.Message}");
            return false;
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("⚠️ Connection to ReportServer timed out");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Unexpected error connecting to ReportServer: {ex.Message}");
            return false;
        }
    }
}
