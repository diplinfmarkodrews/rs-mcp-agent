using ReportServerRPCClient.Infrastructure;

namespace RsMcpServer.Web.Middleware;

public class SessionAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CookieContainerProvider _cookieProvider;

    public SessionAuthorizationMiddleware(RequestDelegate next, CookieContainerProvider cookieProvider)
    {
        _next = next;
        _cookieProvider = cookieProvider;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the session is valid
        if (context.Session.GetString("UserSession") == null)
        {
            // If not, redirect to the login page
            context.Response.StatusCode = 401;
            
            return;
        }
        var sessionId = context.Session.GetString("UserSession");
        // Call the next middleware in the pipeline
        
        await _next(context);
    }
}