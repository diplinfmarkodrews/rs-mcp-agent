using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RSChatApp.Shared.Infrastructure.Mcp.Browser.Interfaces;

namespace RSChatApp.Shared.Infrastructure.Mcp.Browser.Middleware;

public class BrowserSessionMiddleware
{
    private const string SessionSentinelKey = "__initial";
    private readonly RequestDelegate _next;
    private readonly ILogger<BrowserSessionMiddleware> _logger;
    public BrowserSessionMiddleware(RequestDelegate next,
        ILogger<BrowserSessionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context, IBrowserInstanceStore browserInstanceStore)
    {
        // Middleware to ensure a browser instance is associated with the current session
        _logger.LogDebug("BrowserSessionMiddleware invoked for {Path}", context.Request.Path);
        
        if (ShouldSkipBrowserInstance(context))
        {
            _logger.LogDebug("Skipping browser instance creation for path: {Path}", context.Request.Path);
            await _next(context);;
            return ;
        }

        var session = context.Session;
        if (!session.TryGetValue(SessionSentinelKey, out _))
        {
            session.SetString(SessionSentinelKey, session.Id);
            _logger.LogDebug("Session initialized ID: {SessionId}", session.Id);
           
        }
        
        _ = await browserInstanceStore.GetOrCreateBrowserInstanceAsync(session.Id);
        _logger.LogInformation("Browser instance ensured for session {SessionId}", session.Id);
        // Call the next middleware in the pipeline
        await _next(context);
    }
    private static bool ShouldSkipBrowserInstance(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        if (string.IsNullOrEmpty(path)) return false;
        if (path == "/") return false; // Main page
       
        // Skip static resources
        if (path.StartsWith("/_framework/") ||
            // path.StartsWith("/_content/") ||
            path.StartsWith("/css/") ||
            path.StartsWith("/js/") ||
            path.StartsWith("/favicon.ico") ||
            path.StartsWith("/health") ||
            path.Contains(".css") ||
            path.Contains(".js") ||
            path.Contains(".map") ||
            path.Contains(".woff") ||
            path.Contains(".ttf") ||
            path.Contains(".png") ||
            path.Contains(".jpg") ||
            path.Contains(".ico"))
        {
            return true;
        }
        
        // Skip Blazor-specific endpoints that shouldn't create browser instances
        if (//path.StartsWith("/_blazor/connect") ||           // SignalR hub endpoints
            path.StartsWith("/_blazor/disconnect") ||
            path.StartsWith("/blazorhub") ||           // Custom hub names
            path.Contains("/negotiate") ||             // SignalR negotiate
            path.StartsWith("/api/") ||                // API calls
            path.StartsWith("/hubs/") ||               // SignalR hubs
            path.StartsWith("/_vs/") ||                // Visual Studio browser link
            path.StartsWith("/hot-reload/") ||         // Hot reload endpoints
            path.Contains("/circuitpack") )//||           // Blazor circuit pack
            //context.Request.Headers.ContainsKey("X-Requested-With") || // AJAX requests
            //context.Request.ContentType?.Contains("application/json") == true) // JSON requests
        {
            return true;
        }
        
        // Skip if this is a SignalR connection
        if (context.Request.Headers.ContainsKey("Connection") && 
            context.Request.Headers["Connection"].ToString().Contains("Upgrade"))
        {
            return true;
        }
        
        return false;
    }
}