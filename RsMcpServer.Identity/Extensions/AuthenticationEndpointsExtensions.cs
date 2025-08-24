using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using RsMcpServer.Identity.Services;
using RsMcpServer.Identity.Models.Requests;
using RsMcpServer.Identity.Models.Responses;
using System.Security.Claims;

namespace RsMcpServer.Identity.Extensions;

/// <summary>
/// Extension methods for registering authentication minimal API endpoints
/// </summary>
public static class AuthenticationEndpointsExtensions
{
    /// <summary>
    /// Map authentication endpoints for Legacy authentication and existing Keycloak integration
    /// </summary>
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/auth")
            .WithTags("Authentication");

        // V1 endpoints (Legacy ReportServer authentication for direct access)
        var v1 = auth.MapGroup("/v1")
            .WithTags("Authentication V1 - Legacy");

        v1.MapPost("/login", LoginV1Async)
            .WithName("LoginV1")
            .WithSummary("Login using Legacy ReportServer authentication")
            .Produces<LoginResponse>()
            .Produces<LoginResponse>(400);

        v1.MapPost("/refresh", RefreshV1Async)
            .WithName("RefreshV1")
            .WithSummary("Refresh Legacy authentication token")
            .Produces<RefreshResponse>()
            .Produces<RefreshResponse>(400);

        v1.MapPost("/logout", LogoutV1Async)
            .WithName("LogoutV1")
            .WithSummary("Logout from Legacy authentication")
            .Produces<LogoutResponse>();

        // Common endpoints
        auth.MapGet("/me", GetCurrentUserAsync)
            .WithName("GetCurrentUser")
            .WithSummary("Get current authenticated user information (works with both Legacy tokens and Keycloak via existing middleware)")
            .Produces<UserInfo>()
            .Produces(401)
            .RequireAuthorization(); // This will use the existing middleware for Keycloak or our new middleware for Legacy

        return endpoints;
    }

    private static async Task<IResult> LoginV1Async(
        [FromBody] LoginRequest request,
        [FromServices] IAuthenticationService authService)
    {
        var result = await authService.AuthenticateAsync(
            request.Username, 
            request.Password);

        if (result.Success)
        {
            var userInfo = CreateUserInfo(result.User!);
            var response = new LoginResponse(
                true, 
                result.Token, 
                userInfo, 
                result.ExpiresAt, 
                null);
            
            return Results.Ok(response);
        }

        return Results.BadRequest(new LoginResponse(false, null, null, null, result.ErrorMessage));
    }

    private static async Task<IResult> RefreshV1Async(
        [FromBody] RefreshRequest request,
        [FromServices] IAuthenticationService authService)
    {
        var result = await authService.RefreshTokenAsync(request.Token);

        if (result.Success)
        {
            var response = new RefreshResponse(true, result.Token, result.ExpiresAt, null);
            return Results.Ok(response);
        }

        return Results.BadRequest(new RefreshResponse(false, null, null, result.ErrorMessage));
    }

    private static async Task<IResult> LogoutV1Async(
        [FromBody] LogoutRequest request,
        [FromServices] IAuthenticationService authService)
    {
        var success = await authService.LogoutAsync(request.Token);
        var response = new LogoutResponse(success, success ? null : "Logout failed");
        
        return Results.Ok(response);
    }

    private static Task<IResult> GetCurrentUserAsync(
        HttpContext context,
        [FromServices] IAuthenticationService authService)
    {
        // This endpoint uses the existing middleware/SessionBridge for authentication
        var user = context.User;
        
        if (!user.Identity?.IsAuthenticated == true)
        {
            return Task.FromResult(Results.Unauthorized());
        }

        var userInfo = CreateUserInfo(user);
        return Task.FromResult(Results.Ok(userInfo));
    }

    private static UserInfo CreateUserInfo(ClaimsPrincipal user)
    {
        var roles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();

        var properties = new Dictionary<string, object>();
        
        // Add custom claims as properties
        foreach (var claim in user.Claims.Where(c => 
            c.Type != ClaimTypes.NameIdentifier && 
            c.Type != ClaimTypes.Name && 
            c.Type != ClaimTypes.Email && 
            c.Type != ClaimTypes.GivenName && 
            c.Type != ClaimTypes.Surname && 
            c.Type != ClaimTypes.Role))
        {
            properties[claim.Type] = claim.Value;
        }

        return new UserInfo(
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
            user.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
            user.FindFirst(ClaimTypes.Email)?.Value,
            user.FindFirst(ClaimTypes.GivenName)?.Value,
            user.FindFirst(ClaimTypes.Surname)?.Value,
            roles,
            properties);
    }
}
