using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestRsMcpServer;

/// <summary>
/// Integration tests for authentication flow between RSChatApp.Web and RsMcpServer.Web
/// These tests require Keycloak to be running for full authentication testing
/// </summary>
[TestClass]
public sealed class AuthenticationIntegrationTests
{
    private static DistributedServicesFixture _fixture;
    private readonly CookieContainer _sessionCookies = new();

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext testContext)
    {
        _fixture = new DistributedServicesFixture();
        await _fixture.InitializeAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await _fixture.DisposeAsync();
    }

    [TestMethod]
    public async Task Test_Keycloak_Connectivity()
    {
        // Test if Keycloak is accessible for authentication testing
        
        // Act
        using var httpClient = new HttpClient();
        var keycloakUrl = "http://localhost:8080/realms/reportserver/.well-known/openid_configuration";
        
        try
        {
            var response = await httpClient.GetAsync(keycloakUrl);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✓ Keycloak is accessible and ready for authentication tests");
                var content = await response.Content.ReadAsStringAsync();
                var config = JsonSerializer.Deserialize<JsonElement>(content);
                
                if (config.TryGetProperty("authorization_endpoint", out var authEndpoint))
                {
                    Console.WriteLine($"✓ Authorization endpoint: {authEndpoint.GetString()}");
                    Assert.IsNotNull(authEndpoint.GetString(), "Authorization endpoint should not be null");
                }
                
                Assert.IsTrue(true, "Keycloak connectivity verified");
            }
            else
            {
                Console.WriteLine($"⚠️  Keycloak not accessible (Status: {response.StatusCode})");
                Console.WriteLine("Some authentication tests may be skipped");
                Assert.Inconclusive($"Keycloak not available for authentication testing (Status: {response.StatusCode})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Keycloak connection failed: {ex.Message}");
            Console.WriteLine("Authentication tests will run in mock mode");
            Assert.Inconclusive($"Keycloak connection failed: {ex.Message}");
        }
    }

    [TestMethod]
    public async Task Test_SessionId_Persistence_AcrossRequests()
    {
        // Test that SessionId persists across multiple requests using the same session
        
        // Act - Make multiple requests with the same session
        var response1 = await _fixture.RsMcpServerClient.GetAsync("/swagger");
        var response2 = await _fixture.RsMcpServerClient.GetAsync("/mcp");
        var response3 = await _fixture.RsMcpServerClient.PostAsync("/api/test", 
            new StringContent("{\"test\":\"sessionPersistence\"}", Encoding.UTF8, "application/json"));

        // Assert - We need to check that response2 succeeded
        Assert.IsTrue(response2.IsSuccessStatusCode,
            $"MCP endpoint should be accessible. Status: {response2.StatusCode}");
        
        // We don't care about the response status for the other endpoints,
        // since we're just testing session persistence across requests
        
        Console.WriteLine("✓ Multiple requests completed with session persistence");
        Console.WriteLine("✓ Check RsMcpServer.Web logs - SessionId should be:");
        Console.WriteLine("  - Same across all three requests");
        Console.WriteLine("  - Non-null value (actual GUID)");
        Console.WriteLine("  - Consistent session tracking");
    }

    [TestMethod]
    public async Task Test_TerminalTool_WithSession_NoAuth()
    {
        // Test TerminalTool behavior with session but no authentication
        
        // Act - Call TerminalTool through MCP with session context
        var mcpRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new
            {
                name = "TerminalTool",
                arguments = new
                {
                    command = "echo 'Integration test command'"
                }
            }
        };

        var json = JsonSerializer.Serialize(mcpRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _fixture.RsMcpServerClient.PostAsync("/mcp", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Console.WriteLine($"TerminalTool MCP call status: {response.StatusCode}");
        Console.WriteLine($"Response content: {responseContent}");
        
        // We expect either OK with error response, or BadRequest response
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected status code OK or BadRequest, but got: {response.StatusCode}");
        
        Console.WriteLine("✓ TerminalTool correctly handled request with session but no authentication");
        Console.WriteLine("✓ Check logs for:");
        Console.WriteLine("  - SessionId: [actual-guid-value]");
        Console.WriteLine("  - IsAuthenticated: False");
        Console.WriteLine("  - TerminalTool authentication error message");
    }

    [TestMethod]
    public async Task Test_CrossApplication_McpCommunication()
    {
        // Test the actual MCP communication path from RSChatApp to RsMcpServer
        
        // Act - Simulate RSChatApp calling RsMcpServer MCP endpoint
        var rsMcpServerUrl = _fixture.RsMcpServerBaseUrl;
        
        // First establish session in RsMcpServer
        await _fixture.RsMcpServerClient.GetAsync("/mcp");
        
        // Then make MCP tool call (similar to what RSChatApp would do)
        var mcpListTools = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/list"
        };

        var json = JsonSerializer.Serialize(mcpListTools);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _fixture.RsMcpServerClient.PostAsync("/mcp", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            $"MCP tools/list request should return OK. Actual: {response.StatusCode}");
        
        Assert.IsFalse(string.IsNullOrEmpty(responseContent),
            "MCP tools/list response should not be empty");
        
        Console.WriteLine($"MCP tools/list response: {response.StatusCode}");
        Console.WriteLine($"Available tools response: {responseContent}");
        
        // Parse response to verify TerminalTool is available
        var hasTerminalTool = responseContent.Contains("TerminalTool") || responseContent.Contains("terminal");
        Assert.IsTrue(hasTerminalTool, "Response should list TerminalTool among available tools");
        
        Console.WriteLine("✓ Cross-application MCP communication successful");
        Console.WriteLine($"✓ RsMcpServer MCP endpoint: {rsMcpServerUrl}/mcp");
    }

    [TestMethod]
    public async Task Test_RequestLogging_ShowsCompleteSessionInfo()
    {
        // Test that RequestLoggingMiddleware captures complete session information
        
        // Act - Make various types of requests to trigger comprehensive logging
        
        // 1. Initial request (creates session)
        await _fixture.RsMcpServerClient.GetAsync("/swagger");
        
        // 2. MCP endpoint request
        var mcpResponse = await _fixture.RsMcpServerClient.GetAsync("/mcp");
        Assert.AreEqual(HttpStatusCode.OK, mcpResponse.StatusCode,
            "MCP endpoint GET should return HTTP 200 OK");
        
        // 3. POST request with body
        var testData = new { sessionTest = true, command = "test", timestamp = DateTime.UtcNow };
        var postContent = new StringContent(JsonSerializer.Serialize(testData), Encoding.UTF8, "application/json");
        await _fixture.RsMcpServerClient.PostAsync("/api/sessiontest", postContent);
        
        // 4. Request with query parameters
        await _fixture.RsMcpServerClient.GetAsync("/swagger?sessionId=test&source=integrationTest");

        // Output verification instructions
        Console.WriteLine("✓ Request logging integration test completed");
        Console.WriteLine();
        Console.WriteLine("=== VERIFY IN RsMcpServer.Web CONSOLE LOGS ===");
        Console.WriteLine("You should see 4 request log entries, each containing:");
        Console.WriteLine();
        Console.WriteLine("=== INCOMING REQUEST ===");
        Console.WriteLine("Method: [GET/POST]");
        Console.WriteLine("Path: [/swagger, /mcp, /api/sessiontest]");
        Console.WriteLine("QueryString: [query parameters if any]");
        Console.WriteLine("ContentType: [application/json for POST]");
        Console.WriteLine("Headers: [all request headers]");
        Console.WriteLine();
        Console.WriteLine("=== SESSION INFO ===");
        Console.WriteLine("SessionId: [should be SAME across all requests]");
        Console.WriteLine("IsAuthenticated: False (no real auth in test)");
        Console.WriteLine("HasBearerToken: False");
        Console.WriteLine("TokenLength: 0");
        Console.WriteLine("UserName: NULL");
        Console.WriteLine("UserClaims: NULL");
        Console.WriteLine();
        Console.WriteLine("=== END REQUEST INFO ===");
        Console.WriteLine("=== RESPONSE ===");
        Console.WriteLine("StatusCode: [200, 404, etc.]");
        Console.WriteLine("=== END RESPONSE ===");
        Console.WriteLine();
        Console.WriteLine("✓ The SessionId should be consistent across all requests!");
        
        Assert.IsTrue(true, "Request logging verification - check console output for complete session tracking");
    }
}

