using System.Net;

namespace ReportServer.RpcClient.Infrastructure;
public class CookieContainerProvider
{
    public CookieContainer CookieContainer { get => _cookieContainer; }
    private CookieContainer _cookieContainer = new CookieContainer();
    
    public void ClearCookies()
    {
        _cookieContainer = new CookieContainer();
        
    }

    internal void EnsureCookiesLoaded()
    {
        // Here you would implement the logic to load cookies from a persistent store
        // For demonstration, we will just add a dummy cookie
        var dummyCookie = new Cookie("JSESSIONID", "abc123");
        _cookieContainer.Add(dummyCookie);
    }
}