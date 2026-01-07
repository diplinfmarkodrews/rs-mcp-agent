using Newtonsoft.Json;

namespace ReportServer.RpcClient.DTOs;

public class GwtRpcResponse
{
    [JsonProperty("error")]
    public string? Error { get; set; }

    [JsonProperty("success")]
    public bool Success { get; set; }
    
    [JsonIgnore]
    public Exception? Exception { get; set; }
 
    
    public static GwtRpcResponse Fail(Exception? error = null)
        => new GwtRpcResponse
        {
            Success = false,
            Error = error?.Message,
            Exception = error
        };
    

    public static GwtRpcResponse Successful()
        => new GwtRpcResponse
        {
            Success = true,
        };
}
public class GwtRpcResponse<T> : GwtRpcResponse
{
    [JsonProperty("result")]
    public T? Result { get; set; }
    
    public static GwtRpcResponse<T> Successful(T? result = default)
        => new GwtRpcResponse<T>
        {
            Success = true,
            Result = result
        };
        
    public static new GwtRpcResponse<T> Fail(Exception? error = null)
        => new GwtRpcResponse<T>
        {
            Success = false,
            Error = error?.Message,
            Exception = error,
            Result = default(T)
        };
}