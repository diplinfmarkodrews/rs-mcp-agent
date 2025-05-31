using Newtonsoft.Json;

namespace ReportServerRPCClient.DTOs.Authentication;

public class SecurityCheckDto
{
    [JsonProperty("hasPermission")]
    public bool HasPermission { get; set; }

    [JsonProperty("reason")]
    public string Reason { get; set; }
}