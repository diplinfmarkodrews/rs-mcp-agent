using System.Net;
namespace ReportServerRPCClient.Infrastructure;
public class CookieContainerProvider
{
    public CookieContainer CookieContainer { get => _cookieContainer; }
    private CookieContainer _cookieContainer = new CookieContainer();
    
    public void ClearCookies()
    {
        _cookieContainer = new CookieContainer();
    }
}