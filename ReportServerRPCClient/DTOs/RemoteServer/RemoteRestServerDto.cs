using Newtonsoft.Json;

namespace ReportServerRPCClient.DTOs.RemoteServer;

public class RemoteRsRestServerDto : RemoteServerDefinitionDto
{
    [JsonProperty("apikey")]
    public string Apikey { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("username")]
    public string Username { get; set; }

    [JsonProperty("hasApikey")]
    public bool HasApikey { get; set; }

    // Property modification flags (used by GWT)
    [JsonIgnore]
    public bool ApikeyModified { get; set; }

    [JsonIgnore]
    public bool UrlModified { get; set; }

    [JsonIgnore]
    public bool UsernameModified { get; set; }

    [JsonIgnore]
    public bool HasApikeyModified { get; set; }
}