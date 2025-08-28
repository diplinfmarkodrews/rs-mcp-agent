using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using ReportServer.Abstraction.Exceptions;
using ReportServerRPCClient.DTOs;
using ReportServerRPCClient.Infrastructure;

namespace ReportServerRPCClient.Services;

public class ReportServerGwtRpcClientBase : IDisposable
{
    protected readonly HttpClient _httpClient;
    protected readonly string _moduleBaseUrl;
    protected readonly CookieContainer _cookieContainer;

    public RsRestClientBase(HttpClient httpClient,
        CookieContainerProvider cookieProvider)
    {
        _httpClient = httpClient;
        _cookieContainer = cookieProvider.CookieContainer;
        if (_httpClient.BaseAddress is null)
            throw new InvalidOperationException("BaseAddress not set in HTTP client.");

        _moduleBaseUrl = _httpClient.DefaultRequestHeaders.GetValues("X-GWT-Module-Base").FirstOrDefault()
                         ?? throw new InvalidOperationException("Module base URL not set in HTTP client headers.");
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
