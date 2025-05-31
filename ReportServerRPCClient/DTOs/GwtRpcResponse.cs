using Newtonsoft.Json;

namespace ReportServerRPCClient.DTOs;

public class GwtRpcResponse<T>
{
    [JsonProperty("result")]
    public T Result { get; set; }

    [JsonProperty("error")]
    public string Error { get; set; }

    [JsonProperty("success")]
    public bool Success { get; set; }
}