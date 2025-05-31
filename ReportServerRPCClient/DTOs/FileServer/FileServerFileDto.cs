using Newtonsoft.Json;

namespace ReportServerRPCClient.DTOs.FileServer;

public class FileServerFileDto : AbstractFileServerNodeDto
{
    [JsonProperty("contentType")]
    public string ContentType { get; set; }

    [JsonProperty("size")]
    public long Size { get; set; }

    [JsonProperty("data")]
    public string Data { get; set; }

    [JsonProperty("hasContent")]
    public bool HasContent { get; set; }
}