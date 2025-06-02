using Newtonsoft.Json;

namespace ReportServerRPCClient.DTOs.Terminal;

public class AbstractNodeDto
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }
}