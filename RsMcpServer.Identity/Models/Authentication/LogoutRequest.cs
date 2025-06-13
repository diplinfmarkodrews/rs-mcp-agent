namespace RsMcpServer.Identity.Models.Authentication;

/// <summary>
/// Logout request model
/// </summary>
public class LogoutRequest
{
    public string? RedirectUri { get; set; }
    public bool LogoutFromKeycloak { get; set; } = true;
}
