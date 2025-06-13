using System.Text.Json.Serialization;

namespace RsMcpServer.Identity.Models.Users;

/// <summary>
/// User information from Keycloak
/// </summary>
public class UserInfo
{
    [JsonPropertyName("sub")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("preferred_username")]
    public string PreferredUsername { get; set; } = string.Empty;

    [JsonPropertyName("given_name")]
    public string GivenName { get; set; } = string.Empty;

    [JsonPropertyName("family_name")]
    public string FamilyName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("email_verified")]
    public bool EmailVerified { get; set; }

    [JsonPropertyName("groups")]
    public ICollection<string> Groups { get; set; } = new List<string>();

    [JsonPropertyName("roles")]
    public ICollection<string> Roles { get; set; } = new List<string>();
}
