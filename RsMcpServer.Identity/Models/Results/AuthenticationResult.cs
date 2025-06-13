using RsMcpServer.Identity.Models.Users;

namespace RsMcpServer.Identity.Models.Results;

/// <summary>
/// Authentication result for API responses
/// </summary>
public class AuthenticationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public UserInfo? User { get; set; }
    public TokenResponse? Tokens { get; set; }
    public string? SessionId { get; set; }
    public IDictionary<string, object> Claims { get; set; } = new Dictionary<string, object>();
    public int ExpiresIn { get; set; }
    public Dictionary<string,object> AdditionalData { get; set; }
}
