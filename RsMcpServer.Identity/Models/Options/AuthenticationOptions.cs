namespace RsMcpServer.Identity.Models.Options;

/// <summary>
/// Authentication configuration for the applications
/// </summary>
public class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    /// <summary>
    /// Cookie authentication settings
    /// </summary>
    public CookieOptions Cookie { get; set; } = new();

    /// <summary>
    /// Session configuration
    /// </summary>
    public SessionOptions Session { get; set; } = new();

    /// <summary>
    /// JWT token validation settings
    /// </summary>
    public JwtOptions Jwt { get; set; } = new();
}