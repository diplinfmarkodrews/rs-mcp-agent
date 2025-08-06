using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using RsMcpServer.Identity.Services;

namespace RsMcpServer.Identity.Middleware;

/// <summary>
/// Middleware for handling authentication session management
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

    public async Task InvokeAsync(HttpContext context, ISessionBridgeService sessionBridge, ITokenManagementService tokenService)
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
                // Ensure we have a valid bearer token for ReportServer integration
                var token = await sessionBridge.GetBearerTokenAsync();
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("No valid bearer token available for authenticated user: {User}", 
                        context.User.Identity.Name);
                    
                    // Try to refresh the token
                    var refreshResult = await tokenService.RefreshTokenAsync();
                    if (!refreshResult.Success)
                    {
                        _logger.LogWarning("Token refresh failed, clearing session");
                        await sessionBridge.ClearSessionAsync();
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Authentication token expired");
                        return;
                    }
                }
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
            var p when p?.StartsWith("/auth") == true => true,
            var p when p?.StartsWith("/health") == true => true,
            var p when p?.StartsWith("/swagger") == true => true,
            var p when p?.StartsWith("/api/health") == true => true,
            "/" => true,
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
