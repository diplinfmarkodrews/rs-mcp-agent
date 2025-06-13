using System.Text.Json.Serialization;

namespace RsMcpServer.Identity.Models.Results;

/// <summary>
/// OAuth2/OIDC token response from Keycloak
/// </summary>
public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("id_token")]
    public string IdToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_expires_in")]
    public int RefreshExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// Calculated expiration time
    /// </summary>
    public DateTimeOffset ExpiresAt => DateTimeOffset.UtcNow.AddSeconds(ExpiresIn);

    /// <summary>
    /// Calculated refresh token expiration time
    /// </summary>
    public DateTimeOffset RefreshExpiresAt => DateTimeOffset.UtcNow.AddSeconds(RefreshExpiresIn);

    public string SessionState { get; set; }
}
