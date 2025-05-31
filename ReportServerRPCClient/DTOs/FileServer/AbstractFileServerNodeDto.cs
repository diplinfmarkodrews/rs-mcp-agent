using Newtonsoft.Json;

namespace ReportServerRPCClient.DTOs.FileServer;

public abstract class AbstractFileServerNodeDto
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("parentId")]
    public long? ParentId { get; set; }

    [JsonProperty("flags")]
    public int Flags { get; set; }

    [JsonProperty("lastUpdated")]
    public DateTime? LastUpdated { get; set; }

    [JsonProperty("createdOn")]
    public DateTime? CreatedOn { get; set; }
}