using RsMcpServer.Identity.Models.Results;
using RsMcpServer.Identity.Models.Users;
using RsMcpServer.Identity.Models.Authentication;

namespace RsMcpServer.Identity.Services;

/// <summary>
/// Custom authentication service interface for Keycloak integration
/// </summary>
public interface ICustomAuthenticationService
{
    /// <summary>
    /// Authenticate user with username and password
    /// </summary>
    Task<AuthenticationResult> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Refresh authentication tokens
    /// </summary>
    Task<AuthenticationResult> RefreshTokenAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate current session
    /// </summary>
    Task<SessionValidationResult> ValidateSessionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Logout user
    /// </summary>
    Task<bool> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get current user information
    /// </summary>
    Task<UserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get access token for API calls
    /// </summary>
    Task<string?> GetAccessTokenAsync();
}
