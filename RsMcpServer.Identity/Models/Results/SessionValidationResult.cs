using RsMcpServer.Identity.Models.Users;

namespace RsMcpServer.Identity.Models.Results;

/// <summary>
/// Session validation result
/// </summary>
public class SessionValidationResult
{
    public bool IsValid { get; set; }
    public bool RequiresRefresh { get; set; }
    public string? ErrorMessage { get; set; }
    public UserInfo? User { get; set; }
}
