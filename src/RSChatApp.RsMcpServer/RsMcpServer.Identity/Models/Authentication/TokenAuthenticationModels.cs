using System.Security.Claims;

namespace RsMcpServer.Identity.Models.Authentication;

/// <summary>
/// Represents an authentication session
/// </summary>
public class TokenAuthenticatedSession
{
    public string Token { get; set; } = string.Empty;
    public ClaimsPrincipal User { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Result of authentication operations
/// </summary>
public class TokenAuthenticationResult : TokenAuthenticatedSession
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public static TokenAuthenticationResult Successful(string token, ClaimsPrincipal user, DateTime expiresAt) =>
        new() { Success = true, Token = token, User = user, ExpiresAt = expiresAt };

    public static TokenAuthenticationResult Failed(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}
