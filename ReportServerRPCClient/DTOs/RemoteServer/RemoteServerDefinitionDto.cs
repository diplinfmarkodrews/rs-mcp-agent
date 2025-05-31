using Newtonsoft.Json;

namespace ReportServerRPCClient.DTOs.RemoteServer;

public abstract class RemoteServerDefinitionDto
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("key")]
    public string Key { get; set; }

    [JsonProperty("flags")]
    public int Flags { get; set; }

    [JsonProperty("lastUpdated")]
    public DateTime? LastUpdated { get; set; }

    [JsonProperty("createdOn")]
    public DateTime? CreatedOn { get; set; }
}