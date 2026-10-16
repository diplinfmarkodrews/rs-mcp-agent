using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ReportServer.RpcClient.Infrastructure;
using RsMcpServer.Identity.Services;

namespace RsMcpServer.Identity.Middleware;

/// <summary>
/// Makes the caller's credentials available to the ReportServer RPC client for the current request:
/// the legacy JSESSIONID claim, or the Keycloak bearer token (the ReportServer's own JSESSIONID is tracked per Keycloak user).
/// Anonymous requests stay anonymous.
/// </summary>
public class AuthenticatedSessionMiddleware
{
    private readonly RequestDelegate _next;

    public AuthenticatedSessionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        ReportServerSession.CurrentJsessionId.Value = null;
        ReportServerSession.CurrentBearerToken.Value = null;
        ReportServerSession.CurrentBearerKey.Value = null;

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var jsessionId = context.User.FindFirst("JSESSIONID")?.Value;
            if (!string.IsNullOrEmpty(jsessionId))
            {
                ReportServerSession.CurrentJsessionId.Value = jsessionId;
            }
            else
            {
                var token = BearerToken.FromHeader(context.Request.Headers.Authorization.ToString());
                if (token != null && BearerToken.IsJwt(token))
                {
                    ReportServerSession.CurrentBearerToken.Value = token;
                    ReportServerSession.CurrentBearerKey.Value = context.User.FindFirst("sub")?.Value;
                }
            }
        }

        await _next(context);
    }
}

public static class AuthenticatedSessionMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthenticatedSession(this IApplicationBuilder builder)
        => builder.UseMiddleware<AuthenticatedSessionMiddleware>();
}
