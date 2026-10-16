using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RsMcpServer.Identity.Services;

namespace RsMcpServer.Identity.Extensions;

/// <summary>
/// Authentication setup: Keycloak JWT bearer tokens and legacy ReportServer sessions (Bearer GUID or cookie).
/// </summary>
public static class KeycloakAuthenticationExtensions
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var keycloak = configuration.GetSection("Keycloak");
        var authority = keycloak["Authority"]
            ?? throw new InvalidOperationException("Keycloak:Authority configuration is required");
        var audience = keycloak["Audience"];
        var requireHttps = bool.TryParse(keycloak["RequireHttpsMetadata"], out var configured)
            ? configured
            : !environment.IsDevelopment() && !environment.IsEnvironment("Testing");

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = AuthSchemes.MultiAuth;
                options.DefaultChallengeScheme = AuthSchemes.MultiAuth;
                options.DefaultSignInScheme = AuthSchemes.Cookie;
            })
            .AddPolicyScheme(AuthSchemes.MultiAuth, AuthSchemes.MultiAuth, options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var token = BearerToken.FromHeader(context.Request.Headers.Authorization.ToString());
                    if (token == null)
                        return AuthSchemes.Cookie;
                    return BearerToken.IsJwt(token)
                        ? JwtBearerDefaults.AuthenticationScheme
                        : AuthSchemes.Legacy;
                };
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = authority;
                options.RequireHttpsMetadata = requireHttps;
                options.MapInboundClaims = false;
                options.TokenValidationParameters.ValidateAudience = !string.IsNullOrEmpty(audience);
                options.TokenValidationParameters.ValidAudience = audience;
                options.TokenValidationParameters.NameClaimType = "preferred_username";
            })
            .AddScheme<AuthenticationSchemeOptions, LegacyTokenAuthenticationHandler>(AuthSchemes.Legacy, null)
            .AddCookie(AuthSchemes.Cookie, options =>
            {
                options.Cookie.Name = "RsMcpServer.AuthCookie";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                // API server: never redirect to a login page
                options.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());

        // Legacy login uses the ASP.NET session id as token
        services.AddDistributedMemoryCache();
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromHours(8);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
        });

        return services;
    }

    public static WebApplication UseKeycloakAuthentication(this WebApplication app)
    {
        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
