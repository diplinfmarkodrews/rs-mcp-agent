using Newtonsoft.Json;

namespace ReportServerRPCClient.DTOs;

public class GwtRpcRequest
{
    [JsonProperty("method")]
    public string Method { get; set; }

    [JsonProperty("parameters")]
    public object[] Parameters { get; set; }

    [JsonProperty("service")]
    public string Service { get; set; }
}