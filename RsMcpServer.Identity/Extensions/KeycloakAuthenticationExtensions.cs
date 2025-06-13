using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;
using ReportServerRPCClient.Extensions;
using RsMcpServer.Identity.Models.Authentication;
using RsMcpServer.Identity.Models.Options;
using RsMcpServer.Identity.Services;

namespace RsMcpServer.Identity.Extensions;

/// <summary>
/// Extension methods for configuring sophisticated Keycloak authentication
/// </summary>
public static class KeycloakAuthenticationExtensions
{
    /// <summary>
    /// Adds comprehensive Keycloak authentication with ReportServer integration
    /// </summary>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services, 
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Configure Keycloak options
        services.Configure<KeycloakOptions>(configuration.GetSection("Keycloak"));
        services.Configure<ReportServerOptions>(configuration.GetSection("ReportServer"));
        
        // Add HTTP clients
        services.AddHttpClient("keycloak", client =>
        {
            var authority = configuration["Keycloak:Authority"];
            if (!string.IsNullOrEmpty(authority))
            {
                client.BaseAddress = new Uri(authority);
            }
        });
        var reportServerAddress = configuration["ReportServer:Address"]
            ?? throw new ArgumentNullException("ReportServer:Address", "Report Server address is not configured.");
        services.AddReportServerRpcClient(reportServerAddress);

        // Add authentication services
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.Name = "RSAuth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = environment.IsDevelopment() 
                ? Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest 
                : Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.LoginPath = "/auth/login";
            options.LogoutPath = "/auth/logout";
            options.AccessDeniedPath = "/auth/access-denied";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            
            options.Events.OnSigningIn = context =>
            {
                // Store authentication timestamp
                context.Properties.SetString("auth_time", DateTimeOffset.UtcNow.ToString("O"));
                return Task.CompletedTask;
            };
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Authority = configuration["Keycloak:Authority"];
            options.ClientId = configuration["Keycloak:ClientId"];
            options.ClientSecret = configuration["Keycloak:ClientSecret"];
            options.RequireHttpsMetadata = !environment.IsDevelopment();
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.UsePkce = true; // Enhanced security
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            
            // Configure scopes
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("roles");
            options.Scope.Add("offline_access"); // For refresh tokens
            
            // Custom scopes for ReportServer
            var customScopes = configuration.GetSection("Keycloak:Scopes").Get<string[]>();
            if (customScopes != null)
            {
                foreach (var scope in customScopes)
                {
                    options.Scope.Add(scope);
                }
            }

            // Enhanced event handlers
            options.Events = new OpenIdConnectEvents
            {
                OnTokenValidated = async context =>
                {
                    var authService = context.HttpContext.RequestServices
                        .GetRequiredService<IKeycloakAuthenticationService>();
                    
                    // Initialize ReportServer session
                    await authService.InitializeReportServerSessionAsync(context.Principal!);
                },
                
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<OpenIdConnectEvents>>();
                    
                    logger.LogError(context.Exception, "OIDC authentication failed: {Error}", 
                        context.Exception?.Message);
                    
                    context.HandleResponse();
                    context.Response.Redirect("/auth/error");
                    return Task.CompletedTask;
                },
                
                OnUserInformationReceived = context =>
                {
                    // Log successful user info retrieval
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<OpenIdConnectEvents>>();
                    
                    logger.LogInformation("User information received successfully");
                    
                    return Task.CompletedTask;
                },
                
                OnRedirectToIdentityProvider = context =>
                {
                    // Add custom parameters if needed
                    if (context.Request.Path.StartsWithSegments("/admin"))
                    {
                        context.ProtocolMessage.SetParameter("kc_idp_hint", "admin");
                    }
                    
                    return Task.CompletedTask;
                },
                
                OnTokenResponseReceived = async context =>
                {
                    var authService = context.HttpContext.RequestServices
                        .GetRequiredService<IKeycloakAuthenticationService>();
                    
                    // Store tokens for refresh and ReportServer integration
                    await authService.StoreTokensAsync(context.TokenEndpointResponse);
                }
            };
        });

        // Add authorization policies
        services.AddAuthorizationBuilder()
            .AddPolicy("AuthenticatedUser", policy =>
                policy.RequireAuthenticatedUser())
            .AddPolicy("ReportServerUser", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("scope", "reportserver"))
            .AddPolicy("ReportServerAdmin", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("rs-admin"))
            .AddPolicy("ChatAppUser", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("scope", "chatapp"));

        // Register core services
        services.AddHttpContextAccessor(); // Required for session management
        services.AddScoped<IKeycloakAuthenticationService, KeycloakAuthenticationService>();
        services.AddScoped<IReportServerAuthenticationService, ReportServerAuthenticationService>();
        services.AddScoped<ITokenManagementService, TokenManagementService>();
        services.AddScoped<ISessionBridgeService, SessionBridgeService>();

        // Add session services for token storage
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

    /// <summary>
    /// Configures the authentication middleware pipeline
    /// </summary>
    public static WebApplication UseKeycloakAuthentication(this WebApplication app)
    {
        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();

        // Add authentication endpoints
        app.MapAuthenticationEndpoints();

        return app;
    }

    /// <summary>
    /// Maps authentication-related endpoints
    /// </summary>
    private static void MapAuthenticationEndpoints(this WebApplication app)
    {
        var authGroup = app.MapGroup("/auth").WithTags("Authentication");

        // Direct login endpoint for API clients
        authGroup.MapPost("/login", async (
            LoginRequest request,
            IKeycloakAuthenticationService authService,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { Error = "Username and password are required" });
            }

            var result = await authService.AuthenticateAsync(request, cancellationToken);
            
            if (result.Success)
            {
                return Results.Ok(new
                {
                    Success = true,
                    Message = "Authentication successful",
                    User = result.User,
                    ExpiresIn = result.ExpiresIn
                });
            }

            return Results.Unauthorized();
        })
        .WithName("DirectLogin")
        .AllowAnonymous();

        // OIDC challenge endpoint
        authGroup.MapGet("/challenge", (string? returnUrl) =>
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = returnUrl ?? "/"
            };
            
            return Results.Challenge(properties, [OpenIdConnectDefaults.AuthenticationScheme]);
        })
        .WithName("OIDCChallenge")
        .AllowAnonymous();

        // Logout endpoint
        authGroup.MapPost("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            
            return Results.Ok(new { Message = "Logged out successfully" });
        })
        .WithName("Logout")
        .RequireAuthorization();

        // Token refresh endpoint
        authGroup.MapPost("/refresh", async (
            ITokenManagementService tokenService,
            CancellationToken cancellationToken) =>
        {
            var result = await tokenService.RefreshTokenAsync(cancellationToken);
            
            if (result.Success)
            {
                return Results.Ok(new
                {
                    Success = true,
                    ExpiresIn = result.ExpiresIn
                });
            }

            return Results.Unauthorized();
        })
        .WithName("RefreshToken")
        .RequireAuthorization();

        // User info endpoint
        authGroup.MapGet("/userinfo", (ClaimsPrincipal user) =>
        {
            return Results.Ok(new
            {
                IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
                Name = user.Identity?.Name,
                Claims = user.Claims.Select(c => new { c.Type, c.Value })
            });
        })
        .WithName("UserInfo")
        .RequireAuthorization();
    }
}
