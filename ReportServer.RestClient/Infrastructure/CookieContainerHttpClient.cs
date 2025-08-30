using System.Net;

namespace ReportServer.RestClient.Infrastructure;

public class CookieContainerHttpClient : HttpClient
{
    private readonly CookieContainerProvider _cookieContainerProvider;

    public CookieContainerHttpClient(CookieContainerProvider cookieContainerProvider, HttpMessageHandler handler) : base(handler)
    {
        _cookieContainerProvider = cookieContainerProvider;
        
    }

    public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Add("Cookie", _cookieContainerProvider.CookieContainer.GetCookies(BaseAddress).ToString());
        return base.SendAsync(request, cancellationToken);
    }
}