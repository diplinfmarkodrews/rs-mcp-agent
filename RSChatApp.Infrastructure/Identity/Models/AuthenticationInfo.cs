using System.Security.Claims;

namespace RSChatApp.Infrastructure.Models.Authentication;

/// <summary>
/// Information about the current authentication state
/// </summary>
public class AuthenticationInfo
{
    public bool IsAuthenticated { get; set; }
    public string UserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
    public ClaimsPrincipal? User { get; set; }
    public string Error { get; set; }
}