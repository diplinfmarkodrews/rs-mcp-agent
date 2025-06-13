namespace RsMcpServer.Identity.Models.Options;

public class SessionOptions
{
    /// <summary>
    /// Session timeout in minutes
    /// </summary>
    public int TimeoutMinutes { get; set; } = 120;

    /// <summary>
    /// Session cookie name
    /// </summary>
    public string CookieName { get; set; } = "RsMcpServer.Session";

    /// <summary>
    /// Enable session
    /// </summary>
    public bool Enabled { get; set; } = true;
}