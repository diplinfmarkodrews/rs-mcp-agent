namespace RsMcpServer.Identity.Models.Results;


/// <summary>
/// Session bridge result for ReportServer integration
/// </summary>
public class SessionBridgeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ReportServerSessionId { get; set; }
    public Dictionary<string, string> Cookies { get; set; } = new();
}
