using System.Security.Claims;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RsMcpServer.Identity.Models.Authentication;
using RsMcpServer.Identity.Models.Results;
using RsMcpServer.Identity.Models.Users;


namespace RsMcpServer.Identity.Services;

/// <summary>
/// Interface for Keycloak authentication operations
/// </summary>
public interface IKeycloakAuthenticationService
{
    /// <summary>
    /// Authenticate user with username and password
    /// </summary>
    Task<AuthenticationResult> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initialize ReportServer session after OIDC authentication
    /// </summary>
    Task<SessionBridgeResult> InitializeReportServerSessionAsync(ClaimsPrincipal principal);

    /// <summary>
    /// Store tokens from OIDC response
    /// </summary>
    Task StoreTokensAsync(OpenIdConnectMessage tokenResponse);

    /// <summary>
    /// Get current user information
    /// </summary>
    Task<UserInfo?> GetCurrentUserAsync();

    /// <summary>
    /// Check if current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Get current access token
    /// </summary>
    Task<string?> GetAccessTokenAsync();

    /// <summary>
    /// Sign out user from both Keycloak and local session
    /// </summary>
    Task SignOutAsync();
}

/// <summary>
/// Interface for ReportServer authentication operations
/// </summary>
public interface IReportServerAuthenticationService
{
    /// <summary>
    /// Authenticate with ReportServer using Keycloak token
    /// </summary>
    Task<SessionBridgeResult> AuthenticateWithKeycloakTokenAsync(string accessToken);

    /// <summary>
    /// Get current ReportServer session ID
    /// </summary>
    string? GetCurrentSessionId();

    /// <summary>
    /// Validate ReportServer session
    /// </summary>
    Task<bool> ValidateSessionAsync(string sessionId);

    /// <summary>
    /// Refresh ReportServer session
    /// </summary>
    Task<bool> RefreshSessionAsync();

    /// <summary>
    /// Clear ReportServer session
    /// </summary>
    Task ClearSessionAsync();
}

/// <summary>
/// Interface for token management operations
/// </summary>
public interface ITokenManagementService
{
    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    Task<TokenRefreshResult> RefreshTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if token needs refresh
    /// </summary>
    Task<bool> TokenNeedsRefreshAsync();

    /// <summary>
    /// Get stored access token
    /// </summary>
    Task<string?> GetAccessTokenAsync();

    /// <summary>
    /// Get stored refresh token
    /// </summary>
    Task<string?> GetRefreshTokenAsync();

    /// <summary>
    /// Store tokens securely
    /// </summary>
    Task StoreTokensAsync(TokenResponse tokenResponse);

    /// <summary>
    /// Clear all stored tokens
    /// </summary>
    Task ClearTokensAsync();
}

/// <summary>
/// Interface for session bridging between Keycloak and ReportServer
/// </summary>
public interface ISessionBridgeService
{
    /// <summary>
    /// Synchronize authentication state between systems
    /// </summary>
    Task<bool> SynchronizeSessionsAsync();

    /// <summary>
    /// Handle token refresh across systems
    /// </summary>
    Task HandleTokenRefreshAsync(TokenResponse newTokens);

    /// <summary>
    /// Handle session expiry
    /// </summary>
    Task HandleSessionExpiryAsync();

    /// <summary>
    /// Bridge user session from Keycloak to ReportServer
    /// </summary>
    Task<SessionBridgeResult> BridgeUserSessionAsync(ClaimsPrincipal principal, string accessToken);

    /// <summary>
    /// Get session information
    /// </summary>
    Task<AuthenticationSession?> GetSessionInfoAsync();
}
