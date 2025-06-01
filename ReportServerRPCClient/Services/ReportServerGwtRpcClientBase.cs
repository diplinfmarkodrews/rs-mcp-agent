using System.Net;
using System.Text;
using AutoMapper;
using Newtonsoft.Json;
using ReportServerRPCClient.Infrastructure;

namespace ReportServerRPCClient.Services;

public class ReportServerGwtRpcClientBase : IDisposable
{
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

    protected async Task<string> PostGwtRpcAsync(string servicePath, string payload)
    {
        // Ensure we have a proper URL with scheme
        Uri uri;
        if (Uri.TryCreate(_moduleBaseUrl, UriKind.Absolute, out uri))
        {
            // We have a complete URL with scheme
            var url = $"{_moduleBaseUrl}{servicePath}";
            var content = new StringContent(payload, Encoding.UTF8, "text/x-gwt-rpc");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
        
            return await response.Content.ReadAsStringAsync();
        }
        else
        {
            // The URL doesn't have a scheme, use the HttpClient.BaseAddress instead
            var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? 
                         throw new InvalidOperationException("No valid base URL available");
            var url = $"{baseUrl}/reportserver/{servicePath}";
            var content = new StringContent(payload, Encoding.UTF8, "text/x-gwt-rpc");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
        
            return await response.Content.ReadAsStringAsync();
        }
    }

    protected T ParseGwtResponse<T>(string gwtResponse)
    {
        // GWT responses start with //OK or //EX
        if (gwtResponse.StartsWith("//EX"))
        {
            throw new Exception("Server returned error: " + gwtResponse);
        }

        if (gwtResponse.StartsWith("//OK"))
        {
            // Extract the actual data part
            var dataStart = gwtResponse.IndexOf('[', 4);
            if (dataStart > 0)
            {
                var jsonData = gwtResponse.Substring(dataStart);
                return JsonConvert.DeserializeObject<T>(jsonData);
            }
        }

        throw new InvalidOperationException("Invalid GWT response format");
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
