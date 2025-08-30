using Newtonsoft.Json;

namespace ReportServerRPCClient.DTOs.FileServer;

public class FileServerFolderDto : AbstractFileServerNodeDto
{
    [JsonProperty("children")]
    public List<AbstractFileServerNodeDto> Children { get; set; } = new List<AbstractFileServerNodeDto>();

    [JsonProperty("hasChildren")]
    public bool HasChildren { get; set; }
}