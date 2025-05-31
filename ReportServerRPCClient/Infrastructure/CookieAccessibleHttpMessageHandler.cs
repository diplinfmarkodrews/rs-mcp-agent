using System.Net;

namespace ReportServerRPCClient.Infrastructure;

public class CookieAccessibleHttpMessageHandler : DelegatingHandler
{
    public CookieContainer CookieContainer { get; }

    public CookieAccessibleHttpMessageHandler() : this(new HttpClientHandler())
    {
    }

    public CookieAccessibleHttpMessageHandler(HttpMessageHandler innerHandler) : base(innerHandler)
    {
        CookieContainer = new CookieContainer();
        
        if (InnerHandler is HttpClientHandler handler)
        {
            handler.CookieContainer = CookieContainer;
            handler.UseCookies = true;
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        return await base.SendAsync(request, cancellationToken);
    }
}
