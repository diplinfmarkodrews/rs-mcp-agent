using Newtonsoft.Json;
namespace ReportServerRPCClient.DTOs.RemoteServer;

public abstract class AbstractNodeDto
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("flags")]
    public int Flags { get; set; }

    [JsonProperty("parentId")]
    public long? ParentId { get; set; }
}

public class TreeNodeDto : AbstractNodeDto
{
    [JsonProperty("children")]
    public List<TreeNodeDto> Children { get; set; } = new List<TreeNodeDto>();

    [JsonProperty("hasChildren")]
    public bool HasChildren { get; set; }

    [JsonProperty("expanded")]
    public bool Expanded { get; set; }

    [JsonProperty("leaf")]
    public bool Leaf { get; set; }
}