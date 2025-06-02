using Newtonsoft.Json;

namespace ReportServerRPCClient.DTOs.Terminal;

public class Dto2PosoMapper
{
    [JsonProperty("mappings")]
    public Dictionary<string, string> Mappings { get; set; } = new Dictionary<string, string>();
}