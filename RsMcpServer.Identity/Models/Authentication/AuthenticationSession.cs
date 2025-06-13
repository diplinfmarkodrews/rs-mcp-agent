using RsMcpServer.Identity.Models.Users;

namespace RsMcpServer.Identity.Models.Authentication;


/// <summary>
/// Authentication session information
/// </summary>
public class AuthenticationSession
{
    public string SessionId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? LastActivity { get; set; }
    public UserInfo? User { get; set; }
    public string? ReportServerSessionId { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    
    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
    public bool NeedsRefresh(TimeSpan threshold) => DateTimeOffset.UtcNow.Add(threshold) > ExpiresAt;
}
