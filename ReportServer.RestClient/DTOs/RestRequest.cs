using Newtonsoft.Json;

namespace ReportServer.RestClient.DTOs;

public class RestRequest
{
    [JsonProperty("method")]
    public string Method { get; set; }

    [JsonProperty("parameters")]
    public object[] Parameters { get; set; }

    [JsonProperty("service")]
    public string Service { get; set; }
}