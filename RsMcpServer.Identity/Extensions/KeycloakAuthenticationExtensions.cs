using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Authorization;
using Keycloak.AuthServices.Common;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ReportServer.RpcClient.Extensions;
using RsMcpServer.Identity.Services;

namespace RsMcpServer.Identity.Extensions;

/// <summary>
/// Extension methods for configuring Keycloak authentication using AuthServices
/// </summary>
public static class KeycloakAuthenticationExtensions
{
    /// <summary>
    /// Adds comprehensive Keycloak authentication with ReportServer integration
    /// </summary>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services, 
        IConfiguration configuration,
        IHostEnvironment environment,
        bool setupSessionBridge = false)
    {
        // Read configuration directly from IConfiguration
        var keycloakSection = configuration.GetSection("Keycloak");
        var authority = keycloakSection["Authority"];
        var clientId = keycloakSection["ClientId"] ?? keycloakSection["Resource"];
        var clientSecret = keycloakSection["ClientSecret"];
        var realm = keycloakSection["Realm"];
        
        // More explicit reading of RequireHttpsMetadata with debugging
        var requireHttpsMetadataConfig = keycloakSection["RequireHttpsMetadata"];
        Console.WriteLine($"Debug: RequireHttpsMetadata config value = '{requireHttpsMetadataConfig}'");
        Console.WriteLine($"Debug: Environment = {environment.EnvironmentName}");
        Console.WriteLine($"Debug: IsDevelopment = {environment.IsDevelopment()}");
        Console.WriteLine($"Debug: IsEnvironment('Testing') = {environment.IsEnvironment("Testing")}");
        
        bool requireHttpsMetadata;
        if (!string.IsNullOrEmpty(requireHttpsMetadataConfig))
        {
            if (bool.TryParse(requireHttpsMetadataConfig, out requireHttpsMetadata))
            {
                Console.WriteLine($"Debug: Parsed RequireHttpsMetadata from config = {requireHttpsMetadata}");
            }
            else
            {
                Console.WriteLine($"Debug: Failed to parse RequireHttpsMetadata, using fallback");
                requireHttpsMetadata = !environment.IsDevelopment() && !environment.IsEnvironment("Testing");
            }
        }
        else
        {
            Console.WriteLine($"Debug: No RequireHttpsMetadata config found, using fallback");
            requireHttpsMetadata = !environment.IsDevelopment() && !environment.IsEnvironment("Testing");
        }
        
        Console.WriteLine($"Debug: Final RequireHttpsMetadata = {requireHttpsMetadata}");
        
        if (string.IsNullOrEmpty(authority))
        {
            throw new InvalidOperationException("Keycloak:Authority configuration is required");
        }
        
        if (string.IsNullOrEmpty(clientId))
        {
            throw new InvalidOperationException("Keycloak:ClientId or Keycloak:Resource configuration is required");
        }

        // Debug output
        Console.WriteLine($"Keycloak Configuration: Authority={authority}, ClientId={clientId}, RequireHttpsMetadata={requireHttpsMetadata}, Environment={environment.EnvironmentName}");

        // Skip Keycloak AuthServices for Testing environment or when HTTPS is disabled
        // The AuthServices library seems to have issues with HTTP URLs even when RequireHttpsMetadata=false
        if (!environment.IsEnvironment("Testing") && requireHttpsMetadata)
        {
            try
            {
                services.AddKeycloakWebApiAuthentication(configuration, options =>
                {
                    options.RequireHttpsMetadata = requireHttpsMetadata;
                });
                
                // Add Keycloak authorization
                services.AddKeycloakAuthorization(configuration);
                Console.WriteLine("Keycloak AuthServices configured successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Keycloak AuthServices configuration failed, using manual setup: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Skipping Keycloak AuthServices - using manual OpenIdConnect configuration");
        }

        // Add authentication services with cookie support for web apps
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.Name = "RsMcpServer.AuthCookie";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = environment.IsDevelopment() 
                ? CookieSecurePolicy.SameAsRequest 
                : CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.LoginPath = "/auth/v1/login";
            options.LogoutPath = "/auth/v1/logout";
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
            options.Authority = authority;
            options.ClientId = clientId;
            options.ClientSecret = clientSecret;
            options.RequireHttpsMetadata = requireHttpsMetadata;
            options.ResponseType = OpenIdConnectResponseType.Code;
            
            options.UsePkce = true;
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            
            // Configure scopes
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("roles");
            options.Scope.Add("offline_access");
            
            // Custom scopes for ReportServer
            var customScopes = configuration.GetSection("Keycloak:Scopes").Get<string[]>();
            if (customScopes != null)
            {
                foreach (var scope in customScopes)
                {
                    if (!options.Scope.Contains(scope))
                    {
                        options.Scope.Add(scope);
                    }
                }
            }

            // Enhanced event handlers
            options.Events = new OpenIdConnectEvents
            {
                OnTokenValidated = async context =>
                {
                    var tokenService = context.HttpContext.RequestServices
                        .GetRequiredService<ITokenManagementService>();
                    
                    // Store tokens for ReportServer integration
                    await tokenService.StoreTokensFromContextAsync(context);
                },
                
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<OpenIdConnectEvents>>();
                    
                    logger.LogError(context.Exception, "OIDC authentication failed: {Error}", 
                        context.Exception?.Message);
                    
                    context.HandleResponse();
                    var errorMessage = Uri.EscapeDataString(context.Exception?.Message ?? "Authentication failed");
                    context.Response.Redirect($"/auth/error?message={errorMessage}");
                    return Task.CompletedTask;
                },
                
                OnUserInformationReceived = context =>
                {
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
                }
            };
        });

        // Add authorization policies using Keycloak resource-based authorization
        services.AddAuthorizationBuilder()
            .AddPolicy("AuthenticatedUser", policy =>
                policy.RequireAuthenticatedUser())
            .AddPolicy("ReportServerUser", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRealmRoles("rs-user"))
            .AddPolicy("ReportServerAdmin", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRealmRoles("rs-admin"))
            .AddPolicy("ChatAppUser", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRealmRoles("chat-user"));

        if (setupSessionBridge)
        {
            // Register core services
            services.AddHttpContextAccessor();
            services.AddScoped<ITokenManagementService, TokenManagementService>();
            services.AddScoped<ISessionBridgeService, SessionBridgeService>();
        }
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
