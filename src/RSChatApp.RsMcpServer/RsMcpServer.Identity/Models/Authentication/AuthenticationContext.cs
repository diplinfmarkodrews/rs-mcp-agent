using System.Security.Claims;

namespace RsMcpServer.Identity.Models.Authentication;

/// <summary>
/// Represents the complete authentication context for a request
/// </summary>
public class AuthenticationContext
{
    public bool IsAuthenticated { get; set; }
    public AuthenticationType Type { get; set; } = AuthenticationType.None;
    public string? SessionId { get; set; }
    public string? AuthenticationToken { get; set; }
    public ClaimsPrincipal? User { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    
    /// <summary>
    /// Creates an unauthenticated context
    /// </summary>
    public static AuthenticationContext Unauthenticated() => new() { Type = AuthenticationType.None };
    
    /// <summary>
    /// Creates a Legacy authentication context
    /// </summary>
    public static AuthenticationContext Legacy(string sessionId, string token, ClaimsPrincipal user) => 
        new()
        {
            IsAuthenticated = true,
            Type = AuthenticationType.Legacy,
            SessionId = sessionId,
            AuthenticationToken = token,
            User = user
        };
    
    /// <summary>
    /// Creates a Keycloak authentication context
    /// </summary>
    public static AuthenticationContext Keycloak(string sessionId, string? token, ClaimsPrincipal user) => 
        new()
        {
            IsAuthenticated = true,
            Type = AuthenticationType.Keycloak,
            SessionId = sessionId,
            AuthenticationToken = token,
            User = user
        };
}

/// <summary>
/// Types of authentication supported by the system
/// </summary>
public enum AuthenticationType
{
    /// <summary>No authentication</summary>
    None,
    /// <summary>Legacy ReportServer authentication with GUID tokens</summary>
    Legacy,
    /// <summary>Keycloak OIDC authentication with JWT tokens</summary>
    Keycloak
}
