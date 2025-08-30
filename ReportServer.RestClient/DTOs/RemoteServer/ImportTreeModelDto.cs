using Newtonsoft.Json;
namespace ReportServerRPCClient.DTOs.RemoteServer;

public class ImportTreeModelDto
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("children")]
    public List<ImportTreeModelDto> Children { get; set; } = new List<ImportTreeModelDto>();

    [JsonProperty("hasChildren")]
    public bool HasChildren { get; set; }

    [JsonProperty("parentId")]
    public long? ParentId { get; set; }

    [JsonProperty("flags")]
    public int Flags { get; set; }

    [JsonProperty("importable")]
    public bool Importable { get; set; }

    [JsonProperty("exportable")]
    public bool Exportable { get; set; }
}