namespace RsMcpServer.Identity.Models.Results;

/// <summary>
/// Token refresh result
/// </summary>
public class TokenRefreshResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public TokenResponse? TokenResponse { get; set; }
}