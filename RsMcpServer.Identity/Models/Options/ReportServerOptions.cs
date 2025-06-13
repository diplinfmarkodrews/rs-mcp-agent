namespace RsMcpServer.Identity.Models.Options;

/// <summary>
/// ReportServer configuration options
/// </summary>
public class ReportServerOptions
{
    public string Address { get; set; } = string.Empty;
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromHours(1);
    public string CookieDomain { get; set; } = string.Empty;
    public bool EnableSessionBridge { get; set; } = true;
}