using Newtonsoft.Json;

namespace ReportServer.RestClient.DTOs;

public class RestResponse
{
    [JsonProperty("error")]
    public string Error { get; set; }

    [JsonProperty("success")]
    public bool Success { get; set; }
    public int? StatusCode { get; set; }
    
    [JsonIgnore]
    public Exception Exception { get; set; }
    
    public static RestResponse Fail(Exception? error = null, int? statusCode = null)
        => new RestResponse
        {
            Success = false,
            Error = error?.Message,
            Exception = error,
            StatusCode = statusCode
        };
}
public class RestResponse<T> : RestResponse
{
    [JsonProperty("result")]
    public T Result { get; set; }
    public static RestResponse<T> Successful(T result = default, int? statusCode = null)
        => new RestResponse<T>
        {
            Success = true,
            Result = result
        };
}