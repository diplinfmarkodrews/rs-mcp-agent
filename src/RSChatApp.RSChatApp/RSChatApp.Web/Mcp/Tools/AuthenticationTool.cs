using System.ComponentModel;
using Microsoft.SemanticKernel;
using System.Text;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using RSChatApp.Infrastructure.UserInteraction;
using RSChatApp.Web.Models.Auth;
using RSChatApp.Web.Services.Authentication;
using RsMcpServer.Identity.Models.Requests;

namespace RSChatApp.Web.Mcp.Tools;

public class AuthenticationTool
{
    private readonly ILogger<AuthenticationTool> _logger;
    private readonly IAuthenticationInfoService _authenticationInfoService;
    private readonly IWaitForUserInteraction<LoginRequest, LoginResult> _loginModalService;

    public AuthenticationTool(ILogger<AuthenticationTool> logger, 
        IAuthenticationInfoService authenticationInfoService, 
        IWaitForUserInteraction<LoginRequest, LoginResult> loginModalService)
    {
        _logger = logger;
        _authenticationInfoService = authenticationInfoService;
        _loginModalService = loginModalService;
    }
    
    [KernelFunction, McpServerTool, Description("Checks whether the user is authenticated against the ReportServer and can execute ReportServerMcp tools or not")]
    public async Task<string> IsAuthenticatedAsync()
    {
        
        // Check User authentication status and build a response
        var authResult = await _authenticationInfoService.GetAuthenticationInfoAsync();
        return authResult.IsAuthenticated 
            ? new StringBuilder("User: ")
                .Append(authResult.UserName)
                .Append(" is authenticated with roles: ")
                .AppendJoin(',', authResult.Roles)
                .ToString() 
            : "User is not authenticated";
    }
    
    [KernelFunction, McpServerTool, Description("Requests the user to login when they need to access ReportServer MCP tools but are not authenticated")]
    public async Task<string> LoginUserRequestedAsync()
    {
        // Request login through the service and wait for the result
        try
        {
            var loginSuccessful = await _loginModalService.RequestUserInteractionAsync(new LoginRequest("", ""));
            if (loginSuccessful.Success)
            {
                // Re-check authentication status after successful login
                return await IsAuthenticatedAsync();
            }
            
            return "Login failed: " + loginSuccessful.ErrorMessage;
            
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Error during login request");
            return new StringBuilder("An error occurred while requesting login. Please try again later.")
                .Append(exc.Message)
                .ToString();
        }
    }
}