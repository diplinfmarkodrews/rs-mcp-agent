using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RsMcpServer.Identity.Extensions;
using RsMcpServer.Identity.Services;
using RsMcpServer.Web.Mcp.Tools;
using Microsoft.SemanticKernel;
using ReportServerRPCClient.Extensions;
using RsMcpServer.Web.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestRsMcpServer;

/// <summary>
/// Integration tests using the DistributedServicesFixture to interact with real WebApplication instances
/// </summary>
[TestClass]
public sealed class LiveApplicationIntegrationTests
{
    private static DistributedServicesFixture _fixture;

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
    public async Task Test_RsMcpServer_IsRunning()
    {
        // Act
        var response = await _fixture.RsMcpServerClient.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var statusCode = response.StatusCode;

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, statusCode, 
            $"RsMcpServer health endpoint should return OK. Status: {statusCode}, Content: {content}");
        
        // Verify real port assignment
        var uri = new Uri(_fixture.RsMcpServerBaseUrl);
        Assert.AreNotEqual(80, uri.Port, "Port should be dynamically assigned (not default port 80)");
        Assert.IsTrue(uri.Port > 1024, "Port should be in the dynamic/private range (>1024)");
        
        Console.WriteLine($"✅ RsMcpServer is running at {uri} (Status: {statusCode})");
    }

    [TestMethod]
    public async Task Test_RSChatApp_IsRunning()
    {
        // Act
        var response = await _fixture.RsChatAppClient.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var statusCode = response.StatusCode;

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, statusCode, 
            $"RSChatApp health endpoint should return OK. Status: {statusCode}, Content: {content}");
        
        // Verify real port assignment
        var uri = new Uri(_fixture.RsChatAppBaseUrl);
        Assert.AreNotEqual(80, uri.Port, "Port should be dynamically assigned (not default port 80)");
        Assert.IsTrue(uri.Port > 1024, "Port should be in the dynamic/private range (>1024)");
        
        Console.WriteLine($"✅ RSChatApp is running at {uri} (Status: {statusCode})");
    }

    [TestMethod]
    public async Task Test_Cross_Application_Communication()
    {
        // This test verifies that RSChatApp can reach RsMcpServer over real HTTP
        
        // Act - RSChatApp makes HTTP call to RsMcpServer using real ports
        using var crossAppClient = new HttpClient();
        var rsMcpHealthResponse = await crossAppClient.GetAsync($"{_fixture.RsMcpServerBaseUrl}/health");
        var rsChatHealthResponse = await crossAppClient.GetAsync($"{_fixture.RsChatAppBaseUrl}/health");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, rsMcpHealthResponse.StatusCode, 
            "RsMcpServer health endpoint should be accessible from an external client");
        Assert.AreEqual(HttpStatusCode.OK, rsChatHealthResponse.StatusCode, 
            "RSChatApp health endpoint should be accessible from an external client");

        // Verify they're on different ports
        var rsMcpUri = new Uri(_fixture.RsMcpServerBaseUrl);
        var rsChatUri = new Uri(_fixture.RsChatAppBaseUrl);
        Assert.AreNotEqual(rsMcpUri.Port, rsChatUri.Port, 
            "The two applications should be running on different ports");

        Console.WriteLine($"✅ Cross-application communication successful!");
        Console.WriteLine($"✅ RsMcpServer port: {rsMcpUri.Port}, RSChatApp port: {rsChatUri.Port}");
    }

    [TestMethod]
    public async Task Test_MCP_Endpoint_IsAccessible()
    {
        // Test that the MCP endpoint is accessible
        
        // Act
        var response = await _fixture.RsMcpServerClient.GetAsync("/mcp");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            $"MCP endpoint should return HTTP 200 OK. Actual: {response.StatusCode}");
        
        Assert.IsFalse(string.IsNullOrEmpty(content), "MCP endpoint response should not be empty");
        
        Console.WriteLine($"✅ MCP endpoint accessible (Status: {response.StatusCode})");
        Console.WriteLine($"Response: {content}");
    }
}

