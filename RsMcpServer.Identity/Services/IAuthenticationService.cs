using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ReportServer.Abstraction;
using RsMcpServer.Identity.Models.Authentication;

namespace RsMcpServer.Identity.Services;

/// <summary>
/// Main authentication service for Legacy ReportServer authentication in mcp server
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticate user with credentials
    /// </summary>
    Task<TokenAuthenticationResult> AuthenticateAsync(HttpContext httpContext, string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate authentication token
    /// </summary>
    Task<TokenAuthenticatedSession?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh authentication token
    /// </summary>
    Task<TokenAuthenticationResult> RefreshTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logout and invalidate token
    /// </summary>
    Task<bool> LogoutAsync(string token, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of authentication service for Legacy ReportServer authentication
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IReportServerClient _reportServerClient;
    private readonly ISessionStore _sessionStore;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IReportServerClient reportServerClient,
        ISessionStore sessionStore,
        ILogger<AuthenticationService> logger)
    {
        _reportServerClient = reportServerClient;
        _sessionStore = sessionStore;
        _logger = logger;
    }

    public async Task<TokenAuthenticationResult> AuthenticateAsync(HttpContext httpContext, string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return TokenAuthenticationResult.Failed("Username and password are required");
            }

            _logger.LogDebug("Attempting legacy authentication for user {Username}", username);

            // Authenticate with ReportServer
            var authResult = await _reportServerClient.AuthenticateAsync(username, password);
            
            if (!authResult.IsSuccess || authResult.Data?.IsAuthenticated != true)
            {
                _logger.LogWarning("Legacy authentication failed for user {Username}: {Error}", 
                    username, authResult.Error?.Message ?? "Authentication failed");
                return TokenAuthenticationResult.Failed(authResult.Error?.Message ?? "Authentication failed");
            }
            _logger.LogDebug("Successfully authenticated for user {User}", JsonSerializer.Serialize(authResult.Data));
            var rsAuth = authResult.Data;
            var token = httpContext.Session.Id;
            
            // Create claims principal
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, rsAuth.User.Id.ToString()),
                new(ClaimTypes.Name, username),
                new(ClaimTypes.Email, rsAuth.User.Email ?? string.Empty),
                new(ClaimTypes.GivenName, rsAuth.User.Firstname ?? string.Empty),
                new(ClaimTypes.Surname, rsAuth.User.Lastname ?? string.Empty),
                new("JSESSIONID", rsAuth.SessionId ?? string.Empty),
                new("Token", token),
                new("auth_provider", "Legacy")
            };

            // Add group claims
            if (rsAuth.User.Groups?.Any() == true)
            {
                foreach (var group in rsAuth.User.Groups)
                {
                    claims.Add(new Claim(ClaimTypes.Role, group.Name));
                }
            }

            // Add super user claim
            if (rsAuth.User.SuperUser)
            {
                claims.Add(new Claim("super_user", rsAuth.User.SuperUser.ToString()));
            }

            var identity = new ClaimsIdentity(claims, "Legacy");
            var principal = new ClaimsPrincipal(identity);

            // Calculate expiration (default to 8 hours for ReportServer sessions)
            var expiresAt = DateTime.UtcNow.AddHours(8);
            
            // Create and store session
            var session = new TokenAuthenticatedSession
            {
                Token = token,
                User = principal,
                ExpiresAt = expiresAt,
                Properties = new Dictionary<string, object>
                {
                    ["report_server_session_id"] = rsAuth.SessionId ?? string.Empty,
                    ["user_id"] = rsAuth.User.Id,
                    ["is_super_user"] = rsAuth.User.SuperUser
                }
            };
            await httpContext.SignInAsync(principal);
            await _sessionStore.StoreSessionAsync(token, session, cancellationToken);

            _logger.LogInformation("Legacy authentication successful for user {Username}", username);

            return TokenAuthenticationResult.Successful(token, principal, expiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during legacy authentication for user {Username}", username);
            return TokenAuthenticationResult.Failed("Authentication service error");
        }
    }

    public async Task<TokenAuthenticatedSession?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (!IsValidDotNetSessionTokenFormat(token))
            return null;

        try
        {
            var session = await _sessionStore.GetSessionAsync(token, cancellationToken);
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating legacy token {TokenPrefix}***", 
                token[..Math.Min(8, token.Length)]);
            return null;
        }
    }

    public async Task<TokenAuthenticationResult> RefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (!IsValidDotNetSessionTokenFormat(token))
        {
            return TokenAuthenticationResult.Failed("Invalid token format");
        }

        try
        {
            // Legacy ReportServer doesn't support token refresh
            // We just extend the session if it's still valid
            
            var session = await ValidateTokenAsync(token, cancellationToken);
            if (session == null)
            {
                _logger.LogInformation("No valid session found for legacy token {TokenPrefix}***", 
                    token[..Math.Min(8, token.Length)]);
                return TokenAuthenticationResult.Failed("Invalid or expired token");
            }
            
            var authResult = await _reportServerClient.IsAuthenticatedAsync();
            if (authResult?.Data?.IsAuthenticated == false)
            {
                _logger.LogInformation("ReportServer session is no longer valid for token {TokenPrefix}***", 
                    token[..Math.Min(8, token.Length)]);
                await _sessionStore.RemoveSessionAsync(token, cancellationToken);
                return TokenAuthenticationResult.Failed("Session is no longer valid in ReportServer");
            }

            // Extend session by another 8 hours
            var newExpiresAt = DateTime.UtcNow.AddHours(8);
            session.ExpiresAt = newExpiresAt;
            
            await _sessionStore.StoreSessionAsync(token, session, cancellationToken);

            _logger.LogInformation("Extended legacy session for token {TokenPrefix}***", 
                token[..Math.Min(8, token.Length)]);

            return TokenAuthenticationResult.Successful(token, session.User, newExpiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing legacy token {TokenPrefix}***", 
                token[..Math.Min(8, token.Length)]);
            return TokenAuthenticationResult.Failed("Token refresh error");
        }
    }

    public async Task<bool> LogoutAsync(string token, CancellationToken cancellationToken = default)
    {
        if (!IsValidDotNetSessionTokenFormat(token))
            return false;

        try
        {
            var session = await _sessionStore.GetSessionAsync(token, cancellationToken);
            if (session == null)
                return false;

            // Remove from session store
            await _sessionStore.RemoveSessionAsync(token, cancellationToken);

            // Note: We don't explicitly logout from ReportServer as the session will timeout naturally
            // and we don't want to interfere with other potential ReportServer clients

            _logger.LogInformation("Legacy logout successful for token {TokenPrefix}***", 
                token[..Math.Min(8, token.Length)]);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during legacy logout for token {TokenPrefix}***", 
                token[..Math.Min(8, token.Length)]);
            return false;
        }
    }

    private static bool IsValidDotNetSessionTokenFormat(string token)
    {
        return Guid.TryParse(token, out _); 
    }
}
