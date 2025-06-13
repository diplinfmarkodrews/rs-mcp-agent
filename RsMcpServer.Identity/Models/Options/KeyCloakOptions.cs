namespace RsMcpServer.Identity.Models.Options;

public class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>
    /// Keycloak server authority URL (e.g., https://keycloak.example.com/realms/myrealm)
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2/OIDC client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2/OIDC client secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Realm name for direct API calls
    /// </summary>
    public string Realm { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for Keycloak admin API calls
    /// </summary>
    public string AdminApiBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for Report Server integration
    /// </summary>
    public string ReportServerUrl { get; set; } = string.Empty;

    /// <summary>
    /// Scopes to request during authentication
    /// </summary>
    public ICollection<string> Scopes { get; set; } = new[] { "openid", "profile", "email" };

    /// <summary>
    /// Token endpoint for direct authentication
    /// </summary>
    public string TokenEndpoint => $"{Authority}/protocol/openid-connect/token";

    /// <summary>
    /// User info endpoint
    /// </summary>
    public string UserInfoEndpoint => $"{Authority}/protocol/openid-connect/userinfo";

    /// <summary>
    /// Logout endpoint
    /// </summary>
    public string LogoutEndpoint => $"{Authority}/protocol/openid-connect/logout";

    public TimeSpan TokenRefreshThreshold { get; set; } = TimeSpan.FromMinutes(5);
}
