using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportServerRPCClient.Infrastructure;
using ReportServerPort;
using RsMcpServer.Identity.Models.Options;
using RsMcpServer.Identity.Models.Results;

namespace RsMcpServer.Identity.Services;

/// <summary>
/// Service for integrating authentication with ReportServer
/// </summary>
public class ReportServerAuthenticationService : IReportServerAuthenticationService
{
    private readonly ILogger<ReportServerAuthenticationService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CookieContainerProvider _cookieProvider;
    private readonly IReportServerClient _reportServerClient;
    private readonly ReportServerOptions _options;
    private readonly JwtSecurityTokenHandler _jwtHandler;

    private const string SessionIdKey = "rs:session_id";
    private const string SessionExpiryKey = "rs:session_expiry";

    public ReportServerAuthenticationService(
        ILogger<ReportServerAuthenticationService> logger,
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        CookieContainerProvider cookieProvider,
        IReportServerClient reportServerClient,
        IOptions<ReportServerOptions> options)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _httpClientFactory = httpClientFactory;
        _cookieProvider = cookieProvider;
        _reportServerClient = reportServerClient;
        _options = options.Value;
        _jwtHandler = new JwtSecurityTokenHandler();
    }

    public async Task<SessionBridgeResult> AuthenticateWithKeycloakTokenAsync(string accessToken)
    {
        try
        {
            _logger.LogInformation("Authenticating with ReportServer using Keycloak token");

            // Extract username from JWT token
            var username = ExtractUsernameFromToken(accessToken);
            if (string.IsNullOrEmpty(username))
            {
                return new SessionBridgeResult
                {
                    Success = false,
                    Message = "Cannot extract username from token"
                };
            }

            // For now, we'll use a token-based approach to authenticate with ReportServer
            // In a production environment, you might want to implement a custom authentication
            // mechanism in ReportServer that accepts Keycloak tokens
            
            // Set the authorization header for ReportServer requests
            var httpClient = _httpClientFactory.CreateClient("reportserver");
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            // Store the token in the cookie container for GWT RPC calls
            if (!string.IsNullOrEmpty(_options.Address))
            {
                var uri = new Uri(_options.Address);
                var cookie = new Cookie("AUTH_TOKEN", accessToken, "/", uri.Host)
                {
                    HttpOnly = true,
                    Secure = uri.Scheme == "https"
                };
                _cookieProvider.CookieContainer.Add(cookie);
            }

            // For demonstration, we'll create a pseudo session ID
            // In a real implementation, you'd call ReportServer's authentication endpoint
            var sessionId = GenerateSessionId(username, accessToken);
            
            // Store session information
            await StoreSessionAsync(sessionId);

            _logger.LogInformation("ReportServer session established: {SessionId}", sessionId);

            return new SessionBridgeResult
            {
                Success = true,
                Message = "ReportServer session established",
                ReportServerSessionId = sessionId,
                Cookies = new Dictionary<string, string>
                {
                    ["AUTH_TOKEN"] = accessToken,
                    ["RS_SESSION"] = sessionId
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating with ReportServer");
            
            return new SessionBridgeResult
            {
                Success = false,
                Message = "ReportServer authentication failed"
            };
        }
    }

    public string? GetCurrentSessionId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.Session?.GetString(SessionIdKey);
    }

    public async Task<bool> ValidateSessionAsync(string sessionId)
    {
        try
        {
            // In a real implementation, you would call ReportServer to validate the session
            // For now, we'll check if the session exists and hasn't expired
            
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session == null)
            {
                return false;
            }

            var storedSessionId = httpContext.Session.GetString(SessionIdKey);
            if (storedSessionId != sessionId)
            {
                return false;
            }

            var expiryString = httpContext.Session.GetString(SessionExpiryKey);
            if (string.IsNullOrEmpty(expiryString) || 
                !DateTimeOffset.TryParse(expiryString, out var expiry))
            {
                return false;
            }

            return DateTimeOffset.UtcNow < expiry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating ReportServer session: {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<bool> RefreshSessionAsync()
    {
        try
        {
            var sessionId = GetCurrentSessionId();
            if (string.IsNullOrEmpty(sessionId))
            {
                return false;
            }

            // In a real implementation, you would call ReportServer to refresh the session
            // For now, we'll extend the expiry time
            
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session == null)
            {
                return false;
            }

            var newExpiry = DateTimeOffset.UtcNow.Add(_options.SessionTimeout);
            httpContext.Session.SetString(SessionExpiryKey, newExpiry.ToString("O"));

            _logger.LogInformation("ReportServer session refreshed: {SessionId}, new expiry: {Expiry}", 
                sessionId, newExpiry);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing ReportServer session");
            return false;
        }
    }

    public async Task ClearSessionAsync()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session != null)
            {
                httpContext.Session.Remove(SessionIdKey);
                httpContext.Session.Remove(SessionExpiryKey);
            }

            // Clear cookies
            _cookieProvider.ClearCookies();

            _logger.LogInformation("ReportServer session cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing ReportServer session");
        }
    }

    private string? ExtractUsernameFromToken(string accessToken)
    {
        try
        {
            if (!_jwtHandler.CanReadToken(accessToken))
            {
                return null;
            }

            var jwt = _jwtHandler.ReadJwtToken(accessToken);
            return jwt.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                ?? jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting username from token");
            return null;
        }
    }

    private string GenerateSessionId(string username, string accessToken)
    {
        // Generate a deterministic session ID based on username and token hash
        var tokenHash = accessToken.GetHashCode().ToString("X");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return $"RS_{username}_{tokenHash}_{timestamp}";
    }

    private async Task StoreSessionAsync(string sessionId)
    {
        await Task.CompletedTask; // For async consistency
        
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Session == null)
        {
            _logger.LogWarning("Cannot store ReportServer session: HttpContext or Session is null");
            return;
        }

        try
        {
            httpContext.Session.SetString(SessionIdKey, sessionId);
            
            var expiry = DateTimeOffset.UtcNow.Add(_options.SessionTimeout);
            httpContext.Session.SetString(SessionExpiryKey, expiry.ToString("O"));

            _logger.LogInformation("ReportServer session stored: {SessionId}, expires: {Expiry}", 
                sessionId, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing ReportServer session");
        }
    }
}
