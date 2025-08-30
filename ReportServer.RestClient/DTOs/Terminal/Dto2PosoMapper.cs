using Newtonsoft.Json;

namespace ReportServer.RestClient.DTOs.Terminal;

public class Dto2PosoMapper
{
    [JsonProperty("mappings")]
    public Dictionary<string, string> Mappings { get; set; } = new Dictionary<string, string>();
}