using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Keycloak.AuthServices.Authentication;

namespace RsMcpServer.Identity.Services;

public interface ISessionBridgeService
{
    Task<string?> GetBearerTokenAsync();
    Task<ClaimsPrincipal?> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
    Task ClearSessionAsync();
    Task<string?> GetSessionIdAsync();
}

/// <summary>
/// Service for bridging sessions between Keycloak and ReportServer
/// </summary>
public class SessionBridgeService : ISessionBridgeService
{
    private readonly ILogger<SessionBridgeService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenManagementService _tokenManagement;
    private readonly KeycloakAuthenticationOptions _keycloakOptions;

    public SessionBridgeService(
        ILogger<SessionBridgeService> logger,
        IHttpContextAccessor httpContextAccessor,
        ITokenManagementService tokenManagement,
        IOptions<KeycloakAuthenticationOptions> keycloakOptions)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _tokenManagement = tokenManagement;
        _keycloakOptions = keycloakOptions.Value;
    }

    public async Task<string?> GetBearerTokenAsync()
    {
        try
        {
            _logger.LogDebug("Retrieving bearer token for ReportServer");
            
            var token = await _tokenManagement.GetAccessTokenAsync();
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No valid access token available");
                return null;
            }

            _logger.LogDebug("Bearer token retrieved successfully");
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bearer token");
            return null;
        }
    }

    public async Task<ClaimsPrincipal?> GetCurrentUserAsync()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return httpContext.User;
            }

            _logger.LogDebug("No authenticated user found");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user");
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var user = await GetCurrentUserAsync();
            var token = await GetBearerTokenAsync();
            
            return user?.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authentication status");
            return false;
        }
    }

    public async Task ClearSessionAsync()
    {
        try
        {
            _logger.LogInformation("Clearing session data");
            
            await _tokenManagement.ClearTokensAsync();
            
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session != null)
            {
                httpContext.Session.Clear();
                await httpContext.Session.CommitAsync();
            }

            _logger.LogInformation("Session cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing session");
        }
    }

    public async Task<string?> GetSessionIdAsync()
    {
        try
        {
            _logger.LogDebug("Retrieving session ID");
            
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session == null)
            {
                _logger.LogWarning("No active session found");
                return null;
            }

            // Ensure session is loaded
            await httpContext.Session.LoadAsync();
            
            var sessionId = httpContext.Session.Id;
            
            if (string.IsNullOrEmpty(sessionId))
            {
                _logger.LogWarning("Session ID is null or empty");
                return null;
            }

            _logger.LogDebug("Session ID retrieved successfully: {SessionId}", sessionId);
            return sessionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session ID");
            return null;
        }
    }
}
