namespace RsMcpServer.Identity.Models.Options;

public class JwtOptions
{
    /// <summary>
    /// JWT audience validation
    /// </summary>
    public string ValidAudience { get; set; } = string.Empty;

    /// <summary>
    /// JWT issuer validation
    /// </summary>
    public string ValidIssuer { get; set; } = string.Empty;

    /// <summary>
    /// JWT clock skew in minutes
    /// </summary>
    public int ClockSkewMinutes { get; set; } = 5;
}