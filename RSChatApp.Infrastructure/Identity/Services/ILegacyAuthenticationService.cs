using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RSChatApp.Infrastructure.Identity.Clients;
using RSChatApp.Infrastructure.Models.Authentication;

namespace RSChatApp.Infrastructure.Identity.Services;

public interface ILegacyAuthenticationService
{
    Task<AuthenticationInfo> AuthenticateAsync(string username, string password, CancellationToken cancellationToken);
    Task LogoutAsync(CancellationToken cancellationToken);
}

public class LegacyAuthenticationService : ILegacyAuthenticationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<LegacyAuthenticationService> _logger;
    private readonly IAuthenticationClient _authenticationClient;
    private const string LegacySessionCacheKey = "RsMcpServerLegacySession";

    public LegacyAuthenticationService(ILogger<LegacyAuthenticationService> logger,
        IHttpContextAccessor httpContextAccessor,
        IAuthenticationClient authenticationClient)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _authenticationClient = authenticationClient;
    }
    
    public async Task<AuthenticationInfo> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        var authResult = await _authenticationClient.AuthenticateAsync(username, password, cancellationToken);
        
        if (authResult.Success)
        {
            _logger.LogInformation("User {Username} authenticated successfully on legacy authflow.", username);
            // Create claims from authResult.User
            var claims = CreateClaims(authResult);
            var identity = new ClaimsIdentity(claims, "RsMcpServer");
            var principal = new ClaimsPrincipal(identity);
            var authInfo = new AuthenticationInfo
            {
                IsAuthenticated = true,
                UserName = authResult.User?.Username ?? string.Empty,
                User = principal,
                Roles = authResult.User?.Roles ?? new List<string>()
            };
            // Sign in to RSChatApp
            var httpContext = _httpContextAccessor.HttpContext!;
            await httpContext.Session.LoadAsync(cancellationToken);
            
            await httpContext.SignInAsync(principal, new AuthenticationProperties
            {
                ExpiresUtc = authResult.ExpiresAt
            });
            // Store RsMcpServer token in session
            httpContext.Session.SetString(LegacySessionCacheKey, authResult.Token ?? string.Empty);
            _logger.LogDebug("User authenticated successfully on legacy authflow with token: {Token}.", authResult.Token);
            _logger.LogDebug("User claims: {Claims}.", string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}")));
            _logger.LogInformation("User {Username} authenticated successfully on legacy authflow.", username);
            return authInfo;
        }
        
        _logger.LogWarning("Authentication failed for user {Username}: {Error}", username, authResult.Error);
        
        return new AuthenticationInfo
        {
            UserName = string.Empty,
            User = null,
            Roles = new List<string>(),
            IsAuthenticated = false,
            Error = authResult.Error
        };;
    }

    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext!;
        await httpContext.Session.LoadAsync(cancellationToken);
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            //TODO: Invalidate session on RsMcpServer side + logout RS
            var username = httpContext.User.Identity.Name;
            await httpContext.SignOutAsync();
            httpContext.Session.Remove(LegacySessionCacheKey);
            _logger.LogInformation("User {Username} logged out successfully from legacy authflow.", username);
            return;
        }
        _logger.LogWarning("Logout attempted but no user is authenticated.");
    }

    private static IEnumerable<Claim> CreateClaims(AuthenticationResult authResult)
    {
        if (authResult.User == null)
        {
            return Enumerable.Empty<Claim>();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, authResult.User.Id),
            new Claim(ClaimTypes.Name, authResult.User.Username),
            new Claim(ClaimTypes.Email, authResult.User.Email ?? string.Empty),
            new Claim("FirstName", authResult.User.FirstName ?? string.Empty),
            new Claim("LastName", authResult.User.LastName ?? string.Empty),
            new Claim("Token", authResult.Token)
        };

        // Add roles as claims
        if (authResult.User.Roles != null)
        {
            foreach (var role in authResult.User.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        // Add additional properties as claims
        if (authResult.User.Properties != null)
        {
            foreach (var prop in authResult.User.Properties)
            {
                claims.Add(new Claim(prop.Key, prop.Value));
            }
        }

        return claims;
    }
}