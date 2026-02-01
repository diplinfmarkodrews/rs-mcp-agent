using Newtonsoft.Json;

namespace ReportServer.RpcClient.DTOs;

public class GwtRpcRequest
{
    [JsonProperty("method")]
    public string Method { get; set; }

    [JsonProperty("parameters")]
    public object[] Parameters { get; set; }

    [JsonProperty("service")]
    public string Service { get; set; }
}