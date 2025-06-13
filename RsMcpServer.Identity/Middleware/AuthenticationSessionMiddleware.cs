using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using RsMcpServer.Identity.Services;

namespace RsMcpServer.Identity.Middleware;

/// <summary>
/// Middleware for handling authentication session management
/// </summary>
public class AuthenticationSessionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationSessionMiddleware> _logger;

    public AuthenticationSessionMiddleware(RequestDelegate next, ILogger<AuthenticationSessionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISessionBridgeService sessionBridge)
    {
        // Skip authentication checks for certain paths
        if (ShouldSkipAuthentication(context))
        {
            await _next(context);
            return;
        }

        try
        {
            // Check if user is authenticated
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                // Synchronize sessions to ensure they're still valid
                var syncResult = await sessionBridge.SynchronizeSessionsAsync();
                
                if (!syncResult)
                {
                    _logger.LogWarning("Session synchronization failed for user: {User}", 
                        context.User.Identity.Name);
                    
                    // Handle session expiry
                    await sessionBridge.HandleSessionExpiryAsync();
                    return;
                }
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in authentication session middleware");
            
            // Continue with the request even if session management fails
            await _next(context);
        }
    }

    private static bool ShouldSkipAuthentication(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        
        var skipPaths = new[]
        {
            "/auth/login",
            "/auth/logout", 
            "/auth/challenge",
            "/auth/error",
            "/health",
            "/alive",
            "/_framework",
            "/css",
            "/js",
            "/images",
            "/favicon.ico"
        };

        return skipPaths.Any(skipPath => path.StartsWith(skipPath));
    }
}

/// <summary>
/// Extension methods for registering authentication middleware
/// </summary>
public static class AuthenticationMiddlewareExtensions
{
    /// <summary>
    /// Adds authentication session middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseAuthenticationSession(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthenticationSessionMiddleware>();
    }
}
