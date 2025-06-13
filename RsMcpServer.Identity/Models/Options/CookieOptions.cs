namespace RsMcpServer.Identity.Models.Options;

public class CookieOptions
{
    /// <summary>
    /// Cookie name for authentication
    /// </summary>
    public string Name { get; set; } = "RsMcpServer.Auth";

    /// <summary>
    /// Cookie domain
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Cookie path
    /// </summary>
    public string Path { get; set; } = "/";

    /// <summary>
    /// Cookie expiration in minutes
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Sliding expiration enabled
    /// </summary>
    public bool SlidingExpiration { get; set; } = true;

    /// <summary>
    /// Secure cookie policy
    /// </summary>
    public bool SecurePolicy { get; set; } = true;

    /// <summary>
    /// HttpOnly cookie
    /// </summary>
    public bool HttpOnly { get; set; } = true;

    /// <summary>
    /// SameSite cookie policy
    /// </summary>
    public string SameSite { get; set; } = "Strict";
}
