using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using RsMcpServer.Identity.Services;

namespace RsMcpServer.Identity.Middleware;

/// <summary>
/// Middleware for handling both Legacy token and Keycloak authentication session management
/// </summary>
public class AuthenticatedSessionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticatedSessionMiddleware> _logger;

    public AuthenticatedSessionMiddleware(RequestDelegate next, ILogger<AuthenticatedSessionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context, 
        ISessionBridgeService sessionBridge)
    {
        // Skip authentication checks for certain paths
        if (ShouldSkipAuthentication(context))
        {
            await _next(context);
            return;
        }

        try
        {
            // Single entry point - let SessionBridge handle the complexity
            var authContext = await sessionBridge.GetAuthenticationContextAsync();
            
            if (authContext.IsAuthenticated && authContext.User != null)
            {
                context.User = authContext.User;
                _logger.LogDebug("Authentication successful: {Type} for {User}", 
                    authContext.Type, authContext.User.Identity?.Name);
            }
            else
            {
                _logger.LogDebug("No authentication context available");
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in authentication middleware");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Internal server error");
        }
    }

    private static bool ShouldSkipAuthentication(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        
        return path switch
        {
            var p when p?.StartsWith("/api/auth") == true => true,  // Auth endpoints
            var p when p?.StartsWith("/auth") == true => true,      // Legacy auth paths
            var p when p?.StartsWith("/health") == true => true,    // Health checks
            var p when p?.StartsWith("/swagger") == true => true,   // Swagger UI
            var p when p?.StartsWith("/openapi") == true => true,   // Swagger UI
            var p when p?.StartsWith("/blocklyautomation") == true => true,   // Swagger UI
            var p when p?.StartsWith("/api/health") == true => true, // API health
            "/" => false,                                             // Root
            _ => false
        };
    }
}

/// <summary>
/// Extension methods for registering the authenticated session middleware
/// </summary>
public static class AuthenticatedSessionMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthenticatedSession(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthenticatedSessionMiddleware>();
    }
}
