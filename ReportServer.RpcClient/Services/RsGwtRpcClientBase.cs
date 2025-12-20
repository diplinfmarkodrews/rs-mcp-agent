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

    protected bool TryParseException(string response, out ServerCallFailedException? exception)
    {
        if (response.StartsWith("//EX"))
        {
            var stringTable = ExtractStringTable(response);

            // The error message is typically the second string in the table (after the exception class name)
            var errorMessage = stringTable.Count > 1
                ? stringTable[1]
                : "An error occurred initializing the terminal session";

            var exceptionType = stringTable.Count > 0
                ? stringTable[0].Split('/')[0]
                : "Unknown exception";

            exception = new ServerCallFailedException(errorMessage, exceptionType);
            return true;
        }
        exception = null;
        return false;
    }
    
    // protected ServerCallFailedException? TryParseException(string response)
    // {
    //     // Check for GWT exception response
    //     // Format: //EX[2,0,1,["net.datenwerke.gxtdto.client.servercommunication.exceptions.ViolatedSecurityExceptionDto/668224195","Insufficient rights for: Violated security. Execution of method execute in class net.datenwerke.rs.terminal.server.terminal.TerminalRpcServiceImpl(target: net.datenwerke.rs.terminal.server.terminal.TerminalRpcServiceImpl$$EnhancerByGuice$$79050f51) was prohibited.  "],0,7]
    //     if (response.StartsWith("//EX"))
    //     {
    //         var stringTable = ExtractStringTable(response);
    //
    //         // The error message is typically the second string in the table (after the exception class name)
    //         var errorMessage = stringTable.Count > 1
    //             ? stringTable[1]
    //             : "An error occurred initializing the terminal session";
    //
    //         var exceptionType = stringTable.Count > 0
    //             ? stringTable[0].Split('/')[0]
    //             : "Unknown exception";
    //
    //         return new ServerCallFailedException(errorMessage, exceptionType);
    //     }
    //
    //     return null;
    // }
    
      /// <summary>
    /// Extracts string table from complex GWT serialized response
    /// Format: //OK[...data...,["string1","string2",...],metadata]
    /// </summary>
    protected List<string> ExtractStringTable(string gwtResponse)
    {
        var stringTable = new List<string>();
        
        if (string.IsNullOrEmpty(gwtResponse))
            return stringTable;

        try
        {
            // The GWT response has the string table as a JSON array
            // Format: //OK[5,2,4,2,0,3,2,2,1,["java.util.HashMap/1797211028","java.lang.String/2004016611","pathWay","sessionId","00280fbe-f7ad-492f-8f4a-08952e61645c"],0,7]
            // We need to find the last array that contains quoted strings: ,["..."]
            
            var startIdx = gwtResponse.LastIndexOf(",[\"");
            if (startIdx == -1) return stringTable;
            
            // Find the matching closing bracket for this array
            var endIdx = gwtResponse.IndexOf("]", startIdx + 1);
            if (endIdx == -1) return stringTable;
            
            // Extract just the array content: ["string1","string2",...]
            var arrayContent = gwtResponse.Substring(startIdx + 1, endIdx - startIdx);
            
            // Remove the outer brackets: "string1","string2",...
            var innerContent = arrayContent.Trim('[', ']');
            
            // Split by "," pattern (quote-comma-quote)
            var parts = Regex.Split(innerContent, "\",\"");
            
            // Clean up leading/trailing quotes from first and last elements
            for (int i = 0; i < parts.Length; i++)
            {
                var cleaned = parts[i].Trim('"');
                if (!string.IsNullOrWhiteSpace(cleaned))
                {
                    stringTable.Add(cleaned);
                }
            }
        }
        catch (Exception)
        {
            // Fallback: try simple regex extraction
            var matches = Regex.Matches(gwtResponse, "\"([^\"]+)\"");
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    stringTable.Add(match.Groups[1].Value);
                }
            }
        }

        return stringTable;
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