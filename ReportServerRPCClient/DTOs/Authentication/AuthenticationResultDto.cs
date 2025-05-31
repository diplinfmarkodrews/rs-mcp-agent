using Newtonsoft.Json;

namespace ReportServerRPCClient.DTOs.Authentication;

public class AuthenticationResultDto
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("user")]
    public UserDto User { get; set; }

    [JsonProperty("sessionId")]
    public string SessionId { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    [JsonProperty("loginRestrictions")]
    public LoginRestrictionsDto LoginRestrictionsDto { get; set; }
}
public class UserDto
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("username")]
    public string Username { get; set; }

    [JsonProperty("firstname")]
    public string Firstname { get; set; }

    [JsonProperty("lastname")]
    public string Lastname { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("active")]
    public bool Active { get; set; }

    [JsonProperty("superUser")]
    public bool SuperUser { get; set; }

    [JsonProperty("properties")]
    public Dictionary<string, string> Properties { get; set; }

    [JsonProperty("groups")]
    public List<GroupDto> Groups { get; set; }
}

public class GroupDto
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }
}

public class LoginRestrictionsDto
{
    [JsonProperty("maxLoginAttempts")]
    public int MaxLoginAttempts { get; set; }

    [JsonProperty("accountLockoutDuration")]
    public int AccountLockoutDuration { get; set; }

    [JsonProperty("passwordExpirationDays")]
    public int PasswordExpirationDays { get; set; }
}