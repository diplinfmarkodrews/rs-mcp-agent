using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using ReportServer.Abstraction.Exceptions;
using ReportServer.RpcClient.DTOs;
using ReportServer.RpcClient.Infrastructure;

namespace ReportServer.RpcClient.Services;

public class ReportServerGwtRpcClientBase : IDisposable
{
    protected const string CookieSessionId = "JSESSIONID";
    protected readonly HttpClient _httpClient;
    protected readonly string _moduleBaseUrl;
    protected readonly CookieContainer _cookieContainer;

    public ReportServerGwtRpcClientBase(HttpClient httpClient, 
        CookieContainerProvider cookieProvider)
    {
        _httpClient = httpClient;
        _cookieContainer = cookieProvider.CookieContainer;
        if (_httpClient.BaseAddress is null)
            throw new InvalidOperationException("BaseAddress not set in HTTP client.");
        
        _moduleBaseUrl = _httpClient.DefaultRequestHeaders.GetValues("X-GWT-Module-Base").FirstOrDefault() 
                         ?? throw new InvalidOperationException("Module base URL not set in HTTP client headers.");
    }
    
    protected string BuildGwtRpcPayload(string serviceInterface, string methodName, params object[] parameters)
    {
        var lines = new List<string>
        {
            "7", // GWT RPC version
            "0", // flags
            (parameters.Length + 4).ToString(), // number of strings in string table
            _moduleBaseUrl,
            "strongName", // This needs to be extracted from the actual GWT module
            serviceInterface,
            methodName
        };

        // Add parameters
        foreach (var param in parameters)
        {
            lines.Add(SerializeGwtParameter(param));
        }

        return string.Join("|", lines);
    }

    private string SerializeGwtParameter(object param)
    {
        if (param == null) return "null";
        if (param is string str) return str;
        if (param is long || param is int) return param.ToString();
        if (param is bool boolean) return boolean ? "1" : "0";
        
        // For complex objects (DTOs), serialize as JSON for now
        // In a real implementation, you'd need proper GWT serialization
        return JsonConvert.SerializeObject(param);
    }

    protected async Task<string> PostGwtRpcAsync(string servicePath, string payload, bool extractSessionCookie = false, CancellationToken cancellationToken = default)
    {
        // GWT RPC services are served under /reportserver/<servicePath>
        // BaseAddress is http://localhost:8080/reportserver/
        // So we need to prepend "reportserver/" to get /reportserver/reportserver/<servicePath>
        var fullPath = $"reportserver/{servicePath}";
        var content = new StringContent(payload, Encoding.UTF8, "text/x-gwt-rpc");
        var response = await _httpClient.PostAsync(fullPath, content, cancellationToken);
        
        // Read response body first before checking status - GWT RPC may return application errors with HTTP 200
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        
        // If HTTP status indicates failure and we don't have a GWT response, throw
        if (!response.IsSuccessStatusCode && !responseBody.StartsWith("//"))
        {
            response.EnsureSuccessStatusCode(); // This will throw with the HTTP error
        }
        
        if (extractSessionCookie) 
        {
            // Extract JSESSIONID cookie from response and store it in the cookie container
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
            {
                var headers = setCookieHeaders.FirstOrDefault();
                var headersSplit = headers?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                foreach (var header in headersSplit)
                {
                    if (header.StartsWith(CookieSessionId, StringComparison.OrdinalIgnoreCase))
                    {
                        var jsessionId = header.Substring(CookieSessionId.Length + 1);
                        var uri = _httpClient.BaseAddress ?? throw new InvalidOperationException("BaseAddress not set in HTTP client.");
                        _cookieContainer.Add(uri, new Cookie(CookieSessionId, jsessionId));
                        break;
                    }
                }
            }
        }
        
        return responseBody;
    }
    protected GwtRpcResponse ParseGwtResponse(string gwtResponse)
    {
        if (string.IsNullOrWhiteSpace(gwtResponse))
            return new GwtRpcResponse
            {
                Success = false,
                Exception = new ArgumentException("GWT response cannot be null or empty", nameof(gwtResponse))
            };

        // GWT responses start with //OK or //EX
        if (gwtResponse.StartsWith("//EX"))
        {
            var errorMessage = ExtractErrorMessage(gwtResponse);
            return GwtRpcResponse.Fail(new ServerCallFailedException(errorMessage));
        }

        if (gwtResponse.StartsWith("//OK"))
        {
            return GwtRpcResponse.Successful();
        }

        return 
            GwtRpcResponse.Fail(new InvalidOperationException("Invalid GWT response format"));
        
    }
    protected GwtRpcResponse<T> ParseGwtResponse<T>(string gwtResponse)
    {
        // GWT responses start with //OK or //EX
        if (gwtResponse.StartsWith("//EX"))
        {
            var error = ExtractErrorMessage(gwtResponse);
            return new GwtRpcResponse<T>
            {
                Success = false,
                Error = error,
                Exception = new ServerCallFailedException(error)
            };
        }

        if (gwtResponse.StartsWith("//OK"))
        {
            // Extract the actual data part
            var dataStart = gwtResponse.IndexOf('[', 4);
            if (dataStart > 0)
            {
                var jsonData = gwtResponse.Substring(dataStart);
                return new GwtRpcResponse<T>
                {
                    Success = true,
                    Result = JsonConvert.DeserializeObject<T>(jsonData),
                };
            }
        }

        return new GwtRpcResponse<T>
        {
            Success = false,
            Error = "Invalid GWT response format",
            Exception = new InvalidOperationException("Invalid GWT response format")
        };
    }
    protected string ExtractErrorMessage(string gwtResponse)
    {
        var match = Regex.Match(
            gwtResponse, @"\[""([^""]+)""\]");
        return match.Success ? match.Groups[1].Value : "Unknown error";
    }

    protected string ExtractDataFromGwtResponse(string gwtResponse)
    {
        var dataStart = gwtResponse.IndexOf('[', 4);
        return dataStart > 0 ? gwtResponse.Substring(dataStart) : "{}";
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
            // Dispose other managed resources if any
        }
        
        // Free unmanaged resources if any
    }
}