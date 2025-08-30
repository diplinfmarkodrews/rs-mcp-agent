using System.Net;
namespace ReportServer.RestClient.Infrastructure;
public class CookieContainerProvider
{
    public CookieContainer CookieContainer { get => _cookieContainer; }
    private CookieContainer _cookieContainer = new CookieContainer();
    
    public void ClearCookies()
    {
        _cookieContainer = new CookieContainer();
    }
}