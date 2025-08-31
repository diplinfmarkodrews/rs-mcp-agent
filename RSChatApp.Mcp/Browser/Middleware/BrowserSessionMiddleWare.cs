using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RSChatApp.Mcp.Browser.Configuration;
using RSChatApp.Mcp.Browser.Core;

namespace RSChatApp.Mcp.Browser.Middleware;

public class BrowserSessionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IBrowserInstanceStore _browserInstanceStore;
    private readonly ILogger<BrowserSessionMiddleware> _logger;
 
    public BrowserSessionMiddleware(RequestDelegate next,
        IBrowserInstanceStore browserInstanceStore,
        ILogger<BrowserSessionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _browserInstanceStore = browserInstanceStore;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Middleware to ensure a browser instance is associated with the current session
        _logger.LogDebug("BrowserSessionMiddleware invoked for {Path}", context.Request.Path);
        var sessionId = context.Session.Id;
        if (sessionId != null)
        {
            _logger.LogDebug("Session ID: {SessionId}", sessionId);
            _ = await _browserInstanceStore.GetOrCreateBrowserInstanceAsync(sessionId);
            _logger.LogInformation("Browser instance ensured for session {SessionId}", sessionId);
        }
        else
        {
            _logger.LogWarning("No session ID found in the current context");
        }
        // Call the next middleware in the pipeline
        await _next(context);
    }
    
}