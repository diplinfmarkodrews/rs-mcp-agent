using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Keycloak.AuthServices.Authentication;
using RsMcpServer.Identity.Models.Authentication;

namespace RsMcpServer.Identity.Services;

public interface ISessionBridgeService
{
    /// <summary>
    /// Gets the complete authentication context for the current request
    /// </summary>
    Task<AuthenticationContext> GetAuthenticationContextAsync();
    
    /// <summary>
    /// Gets the appropriate authentication token based on authentication type:
    /// - For Legacy authentication: Returns GUID token
    /// - For Keycloak authentication: Returns JWT access token
    /// </summary>
    Task<string?> GetAuthenticationTokenAsync();
    
    /// <summary>
    /// Gets the appropriate session ID based on authentication type:
    /// - For Legacy authentication: Returns ReportServer session ID (JSESSIONID)
    /// - For Keycloak authentication: Returns ASP.NET Core session ID
    /// - For unauthenticated requests: Returns ASP.NET Core session ID
    /// </summary>
    Task<string?> GetSessionIdAsync();
    
    Task<ClaimsPrincipal?> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
    Task ClearSessionAsync();
}

/// <summary>
/// Service for bridging sessions between Keycloak, Legacy, and ReportServer authentication
/// </summary>
public class SessionBridgeService : ISessionBridgeService
{
    private readonly ILogger<SessionBridgeService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenManagementService _tokenManagement;
    private readonly IAuthenticationService _authenticationService;
    private readonly KeycloakAuthenticationOptions _keycloakOptions;

    public SessionBridgeService(
        ILogger<SessionBridgeService> logger,
        IHttpContextAccessor httpContextAccessor,
        ITokenManagementService tokenManagement,
        IAuthenticationService authenticationService,
        IOptions<KeycloakAuthenticationOptions> keycloakOptions)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _tokenManagement = tokenManagement;
        _authenticationService = authenticationService;
        _keycloakOptions = keycloakOptions.Value;
    }

    public async Task<AuthenticationContext> GetAuthenticationContextAsync()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                _logger.LogDebug("No HTTP context available");
                return AuthenticationContext.Unauthenticated();
            }
            var authProvider = context.User.FindFirst("auth_provider")?.Value;

            if (authProvider == "Legacy")
            {
                // Try Legacy authentication first
                var legacyToken = ExtractLegacyTokenFromRequest(context);
                _logger.LogDebug("Extracted Legacy Token: {legacyToken}", legacyToken);
                if (!string.IsNullOrEmpty(legacyToken))
                {
                    var session = await _authenticationService.ValidateTokenAsync(legacyToken);
                    if (session != null)
                    {
                        var reportServerSessionId = session.User.FindFirst("jsessionid")?.Value ?? string.Empty;
                        _logger.LogDebug("Legacy authentication context created for user {Username}",
                            session.User.Identity?.Name);

                        return AuthenticationContext.Legacy(reportServerSessionId, legacyToken, session.User);
                    }
                }
                return AuthenticationContext.Unauthenticated();
            }

            // Check existing Keycloak authentication
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var aspNetSessionId = await GetAspNetSessionIdAsync();
                var accessToken = await _tokenManagement.GetAccessTokenAsync();
                
                _logger.LogDebug("Keycloak authentication context created for user {Username}", 
                    context.User.Identity?.Name);
                
                return AuthenticationContext.Keycloak(aspNetSessionId ?? string.Empty, accessToken, context.User);
            }

            _logger.LogDebug("No authentication context available");
            return AuthenticationContext.Unauthenticated();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication context");
            return AuthenticationContext.Unauthenticated();
        }
    }

    public async Task<string?> GetAuthenticationTokenAsync()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.User?.Identity?.IsAuthenticated == true)
            {
                var authProvider = context.User.FindFirst("auth_provider")?.Value;
                
                if (authProvider == "Legacy")
                {
                    // Extract Legacy token from request
                    var legacyToken = ExtractLegacyTokenFromRequest(context);
                    _logger.LogDebug("Retrieved Legacy authentication token");
                    return legacyToken;
                }
                else
                {
                    // Get Keycloak JWT access token
                    var accessToken = await _tokenManagement.GetAccessTokenAsync();
                    _logger.LogDebug("Retrieved Keycloak access token");
                    return accessToken;
                }
            }

            _logger.LogDebug("No authentication token available - user not authenticated");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving authentication token");
            return null;
        }
    }

    private async Task<string?> GetAspNetSessionIdAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Session != null)
        {
            await context.Session.LoadAsync();
            return context.Session.Id;
        }
        return null;
    }

    private string? ExtractLegacyTokenFromRequest(HttpContext context)
    {
        // Check Authorization header for Legacy tokens (GUIDs)
        // var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        // if (string.IsNullOrEmpty(authHeader) == false)
        // {
        //     string token;
        //     if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        //     {
        //         token = authHeader[7..]; // Remove "Bearer " prefix
        //     }
        //     else
        //     {
        //         token = authHeader; // Support simple token without Bearer prefix
        //     }
        //
        //     // Only return if it's a GUID (Legacy token format)
        //     if (string.IsNullOrEmpty(token) == false)
        //     {
        //         return token;
        //     }
        // }
          
        var token = context.User.Claims.FirstOrDefault(c => c.Type == "Token")?.Value;            
        _logger.LogDebug("Extracted Token from claims: {Token}", token);


        return token;

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

    public Task<ClaimsPrincipal?> GetCurrentUserAsync()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return Task.FromResult<ClaimsPrincipal?>(httpContext.User);
            }

            _logger.LogDebug("No authenticated user found");
            return Task.FromResult<ClaimsPrincipal?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user");
            return Task.FromResult<ClaimsPrincipal?>(null);
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
            if (httpContext == null)
            {
                _logger.LogWarning("No HTTP context available");
                return null;
            }

            // Determine authentication type by checking user claims
            var user = httpContext.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var authProvider = user.FindFirst("auth_provider")?.Value;
                
                if (authProvider == "Legacy")
                {
                    // For Legacy authentication, return the ReportServer session ID
                    var reportServerSessionId = user.FindFirst("session_id")?.Value;
                    
                    if (!string.IsNullOrEmpty(reportServerSessionId))
                    {
                        _logger.LogDebug("Legacy authentication - ReportServer session ID retrieved: {SessionId}", reportServerSessionId);
                        return reportServerSessionId;
                    }
                    
                    _logger.LogWarning("Legacy authentication but no ReportServer session ID found in claims");
                    return null;
                }
                else
                {
                    // For Keycloak authentication, return the ASP.NET Core session ID
                    if (httpContext.Session != null)
                    {
                        await httpContext.Session.LoadAsync();
                        var aspNetSessionId = httpContext.Session.Id;
                        
                        if (!string.IsNullOrEmpty(aspNetSessionId))
                        {
                            _logger.LogDebug("Keycloak authentication - ASP.NET Core session ID retrieved: {SessionId}", aspNetSessionId);
                            return aspNetSessionId;
                        }
                    }
                    
                    _logger.LogWarning("Keycloak authentication but no ASP.NET Core session available");
                    return null;
                }
            }

            // Fallback: try to get ASP.NET Core session ID for unauthenticated requests
            if (httpContext.Session != null)
            {
                await httpContext.Session.LoadAsync();
                var sessionId = httpContext.Session.Id;
                
                if (!string.IsNullOrEmpty(sessionId))
                {
                    _logger.LogDebug("Unauthenticated request - ASP.NET Core session ID retrieved: {SessionId}", sessionId);
                    return sessionId;
                }
            }

            _logger.LogWarning("No session ID available");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session ID");
            return null;
        }
    }
}
