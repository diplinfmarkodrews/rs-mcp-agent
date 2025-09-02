using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RSChatApp.Mcp.Browser.Interfaces;

namespace RSChatApp.Mcp.Browser.Middleware;

public class BrowserAuthUserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IBrowserInstanceStore _browserInstanceStore;
    private readonly ILogger<BrowserAuthUserMiddleware> _logger;

    public BrowserAuthUserMiddleware(RequestDelegate next,
        IBrowserInstanceStore browserInstanceStore,
        ILogger<BrowserAuthUserMiddleware> logger)
    {
        _next = next;
        _browserInstanceStore = browserInstanceStore;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only create browser instances for authenticated users
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = GetUserIdentifier(context);
            
            if (!string.IsNullOrEmpty(userId))
            {
                var instanceKey = $"user:{userId}";
                _logger.LogDebug("Creating browser instance for authenticated user: {UserId}", userId);
                
                try
                {
                    _ = await _browserInstanceStore.GetOrCreateBrowserInstanceAsync(instanceKey);
                    _logger.LogInformation("Browser instance ensured for user {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create browser instance for user {UserId}", userId);
                }
            }
            else
            {
                _logger.LogWarning("Authenticated user found but no valid user ID could be extracted");
            }
        }
        else
        {
            _logger.LogDebug("Skipping browser instance creation - user not authenticated");
        }

        // Call the next middleware in the pipeline
        await _next(context);
    }

    private static string? GetUserIdentifier(HttpContext context)
    {
        // Try multiple claim types to get a unique user identifier
        return context.User.FindFirst("sub")?.Value
               ?? context.User.FindFirst("preferred_username")?.Value
               ?? context.User.FindFirst("email")?.Value
               ?? context.User.FindFirst("name")?.Value
               ?? context.User.Identity?.Name;
    }
}
