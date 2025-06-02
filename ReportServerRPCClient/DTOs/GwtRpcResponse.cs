using Newtonsoft.Json;
using ReportServerPort.Contracts;
using ReportServerPort.Exceptions;

namespace ReportServerRPCClient.DTOs;

public class GwtRpcResponse
{
    [JsonProperty("error")]
    public string Error { get; set; }

    [JsonProperty("success")]
    public bool Success { get; set; }
    
    [JsonIgnore]
    public Exception Exception { get; set; }
    [JsonIgnore]
    public string Message { get; set; }
    public static GwtRpcResponse Fail(string message, Exception? error = null)
        => new GwtRpcResponse
        {
            Success = false,
            Message = message,
            Error = error?.Message,
            Exception = error
        };
    

    public static GwtRpcResponse Successful(string? message = null)
        => new GwtRpcResponse
        {
            Success = true,
            Message = message
        };
}
public class GwtRpcResponse<T> : GwtRpcResponse
{
    [JsonProperty("result")]
    public T Result { get; set; }
    public static GwtRpcResponse<T> Successful(string? message = null, T result = default)
        => new GwtRpcResponse<T>
        {
            Success = true,
            Message = message,
            Result = result
        };
}