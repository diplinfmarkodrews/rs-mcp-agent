using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RsMcpServer.Identity.Models.Authentication;
using RsMcpServer.Identity.Models.Options;
using RsMcpServer.Identity.Models.Results;

namespace RsMcpServer.Identity.Services;

/// <summary>
/// Service for bridging sessions between Keycloak and ReportServer
/// </summary>
public class SessionBridgeService : ISessionBridgeService
{
    private readonly ILogger<SessionBridgeService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IKeycloakAuthenticationService _keycloakAuth;
    private readonly IReportServerAuthenticationService _reportServerAuth;
    private readonly ITokenManagementService _tokenManagement;
    private readonly KeycloakOptions _keycloakOptions;
    private readonly ReportServerOptions _reportServerOptions;

    public SessionBridgeService(
        ILogger<SessionBridgeService> logger,
        IHttpContextAccessor httpContextAccessor,
        IKeycloakAuthenticationService keycloakAuth,
        IReportServerAuthenticationService reportServerAuth,
        ITokenManagementService tokenManagement,
        IOptions<KeycloakOptions> keycloakOptions,
        IOptions<ReportServerOptions> reportServerOptions)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _keycloakAuth = keycloakAuth;
        _reportServerAuth = reportServerAuth;
        _tokenManagement = tokenManagement;
        _keycloakOptions = keycloakOptions.Value;
        _reportServerOptions = reportServerOptions.Value;
    }

    public async Task<bool> SynchronizeSessionsAsync()
    {
        try
        {
            _logger.LogInformation("Synchronizing authentication sessions");

            // Check if Keycloak authentication is valid
            if (!_keycloakAuth.IsAuthenticated)
            {
                _logger.LogWarning("Keycloak authentication not valid, cannot synchronize");
                return false;
            }

            // Check if token needs refresh
            if (await _tokenManagement.TokenNeedsRefreshAsync())
            {
                var refreshResult = await _tokenManagement.RefreshTokenAsync();
                if (!refreshResult.Success)
                {
                    _logger.LogWarning("Failed to refresh token during synchronization");
                    return false;
                }

                // Handle the new tokens
                await HandleTokenRefreshAsync(refreshResult.TokenResponse!);
            }

            // Validate ReportServer session
            var reportServerSessionId = _reportServerAuth.GetCurrentSessionId();
            if (!string.IsNullOrEmpty(reportServerSessionId))
            {
                var isValid = await _reportServerAuth.ValidateSessionAsync(reportServerSessionId);
                if (!isValid)
                {
                    _logger.LogInformation("ReportServer session invalid, re-establishing");
                    
                    var accessToken = await _tokenManagement.GetAccessTokenAsync();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        await _reportServerAuth.AuthenticateWithKeycloakTokenAsync(accessToken);
                    }
                }
                else
                {
                    // Refresh the session to extend its lifetime
                    await _reportServerAuth.RefreshSessionAsync();
                }
            }

            _logger.LogInformation("Session synchronization completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing sessions");
            return false;
        }
    }

    public async Task HandleTokenRefreshAsync(TokenResponse newTokens)
    {
        try
        {
            _logger.LogInformation("Handling token refresh across systems");

            // Store the new tokens
            await _tokenManagement.StoreTokensAsync(newTokens);

            // If ReportServer integration is enabled, update the session
            if (_reportServerOptions.EnableSessionBridge)
            {
                var reportServerSessionId = _reportServerAuth.GetCurrentSessionId();
                if (!string.IsNullOrEmpty(reportServerSessionId))
                {
                    // Re-authenticate with ReportServer using the new token
                    await _reportServerAuth.AuthenticateWithKeycloakTokenAsync(newTokens.AccessToken);
                }
            }

            _logger.LogInformation("Token refresh handled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling token refresh");
        }
    }

    public async Task HandleSessionExpiryAsync()
    {
        try
        {
            _logger.LogInformation("Handling session expiry");

            // Clear all authentication state
            await _keycloakAuth.SignOutAsync();
            await _reportServerAuth.ClearSessionAsync();
            await _tokenManagement.ClearTokensAsync();

            // Redirect to login if we have an HTTP context
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null && !httpContext.Response.HasStarted)
            {
                httpContext.Response.Redirect("/auth/login");
            }

            _logger.LogInformation("Session expiry handled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling session expiry");
        }
    }

    public async Task<SessionBridgeResult> BridgeUserSessionAsync(ClaimsPrincipal principal, string accessToken)
    {
        try
        {
            _logger.LogInformation("Bridging user session from Keycloak to ReportServer");

            if (!_reportServerOptions.EnableSessionBridge)
            {
                return new SessionBridgeResult
                {
                    Success = true,
                    Message = "Session bridge disabled"
                };
            }

            // Authenticate with ReportServer using the Keycloak token
            var result = await _reportServerAuth.AuthenticateWithKeycloakTokenAsync(accessToken);
            
            if (result.Success)
            {
                _logger.LogInformation("Successfully bridged session to ReportServer: {SessionId}", 
                    result.ReportServerSessionId);
            }
            else
            {
                _logger.LogWarning("Failed to bridge session to ReportServer: {Message}", result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bridging user session");
            
            return new SessionBridgeResult
            {
                Success = false,
                Message = "Session bridge error"
            };
        }
    }

    public async Task<AuthenticationSession?> GetSessionInfoAsync()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var user = await _keycloakAuth.GetCurrentUserAsync();
            var reportServerSessionId = _reportServerAuth.GetCurrentSessionId();

            // Get session timing information
            var authTime = httpContext.User.FindFirst("auth_time")?.Value;
            var createdAt = DateTimeOffset.UtcNow; // Default to now
            
            if (!string.IsNullOrEmpty(authTime) && 
                DateTimeOffset.TryParse(authTime, out var parsedAuthTime))
            {
                createdAt = parsedAuthTime;
            }

            return new AuthenticationSession
            {
                SessionId = httpContext.Session.Id,
                CreatedAt = createdAt,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(8), // Default session lifetime
                LastActivity = DateTimeOffset.UtcNow,
                User = user,
                ReportServerSessionId = reportServerSessionId,
                Properties = new Dictionary<string, object>
                {
                    ["UserAgent"] = httpContext.Request.Headers.UserAgent.ToString(),
                    ["IPAddress"] = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    ["AuthenticationMethod"] = "Keycloak-OIDC"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session info");
            return null;
        }
    }

    public async Task<string?> GetCurrentSessionIdAsync()
    {
        try
        {
            var sessionInfo = await GetSessionInfoAsync();
            return sessionInfo?.ReportServerSessionId ?? _reportServerAuth.GetCurrentSessionId();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current session ID");
            return null;
        }
    }
}
