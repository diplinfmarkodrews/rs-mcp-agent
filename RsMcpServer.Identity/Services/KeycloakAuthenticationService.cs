using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RsMcpServer.Identity.Models.Results;
using RsMcpServer.Identity.Models.Authentication;
using RsMcpServer.Identity.Models.Options;
using RsMcpServer.Identity.Models.Users;


namespace RsMcpServer.Identity.Services;

/// <summary>
/// Enhanced Keycloak authentication service with ReportServer integration
/// </summary>
public class KeycloakAuthenticationService : IKeycloakAuthenticationService
{
    private readonly ILogger<KeycloakAuthenticationService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IReportServerAuthenticationService _reportServerAuth;
    private readonly ITokenManagementService _tokenManagement;
    private readonly KeycloakOptions _keycloakOptions;
    private readonly ReportServerOptions _reportServerOptions;
    private readonly JwtSecurityTokenHandler _jwtHandler;

    public KeycloakAuthenticationService(
        ILogger<KeycloakAuthenticationService> logger,
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        IReportServerAuthenticationService reportServerAuth,
        ITokenManagementService tokenManagement,
        IOptions<KeycloakOptions> keycloakOptions,
        IOptions<ReportServerOptions> reportServerOptions)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _httpClientFactory = httpClientFactory;
        _reportServerAuth = reportServerAuth;
        _tokenManagement = tokenManagement;
        _keycloakOptions = keycloakOptions.Value;
        _reportServerOptions = reportServerOptions.Value;
        _jwtHandler = new JwtSecurityTokenHandler();
    }

    public bool IsAuthenticated => 
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public async Task<AuthenticationResult> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting Keycloak authentication for user: {Username}", request.Username);

            using var httpClient = _httpClientFactory.CreateClient("keycloak");
            
            var tokenRequest = new Dictionary<string, string>
            {
                ["client_id"] = _keycloakOptions.ClientId,
                ["client_secret"] = _keycloakOptions.ClientSecret,
                ["grant_type"] = "password",
                ["username"] = request.Username,
                ["password"] = request.Password,
                ["scope"] = string.Join(" ", _keycloakOptions.Scopes)
            };

            var content = new FormUrlEncodedContent(tokenRequest);
            var response = await httpClient.PostAsync(_keycloakOptions.TokenEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Keycloak authentication failed for user {Username}: {Error}", 
                    request.Username, errorContent);
                
                return new AuthenticationResult
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }

            var tokenJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(tokenJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenResponse == null)
            {
                return new AuthenticationResult
                {
                    Success = false,
                    Message = "Invalid token response from Keycloak"
                };
            }

            // Store tokens
            await _tokenManagement.StoreTokensAsync(tokenResponse);

            // Extract user information from token
            var userInfo = ExtractUserInfoFromToken(tokenResponse.AccessToken);
            
            // Create authentication claims
            var claims = CreateClaimsFromToken(tokenResponse.AccessToken);
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Sign in the user
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = request.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
                    RedirectUri = request.ReturnUrl
                };

                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme, 
                    principal, 
                    authProperties);
            }

            // Initialize ReportServer session if enabled
            SessionBridgeResult? reportServerResult = null;
            if (_reportServerOptions.EnableSessionBridge)
            {
                reportServerResult = await InitializeReportServerSessionAsync(principal);
            }

            _logger.LogInformation("Successfully authenticated user: {Username}", userInfo?.Name);

            return new AuthenticationResult
            {
                Success = true,
                Message = "Authentication successful",
                User = userInfo,
                ExpiresIn = tokenResponse.ExpiresIn,
                SessionId = reportServerResult?.ReportServerSessionId,
                AdditionalData = new Dictionary<string, object>
                {
                    ["TokenType"] = tokenResponse.TokenType,
                    ["Scope"] = tokenResponse.Scope,
                    ["SessionState"] = tokenResponse.SessionState
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Keycloak authentication for user: {Username}", request.Username);
            
            return new AuthenticationResult
            {
                Success = false,
                Message = "Authentication service error"
            };
        }
    }

    public async Task<SessionBridgeResult> InitializeReportServerSessionAsync(ClaimsPrincipal principal)
    {
        try
        {
            _logger.LogInformation("Initializing ReportServer session for user: {Username}", 
                principal.Identity?.Name);

            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                return new SessionBridgeResult
                {
                    Success = false,
                    Message = "No access token available"
                };
            }

            return await _reportServerAuth.AuthenticateWithKeycloakTokenAsync(accessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing ReportServer session");
            
            return new SessionBridgeResult
            {
                Success = false,
                Message = "Failed to initialize ReportServer session"
            };
        }
    }

    public async Task StoreTokensAsync(OpenIdConnectMessage tokenResponse)
    {
        var tokens = new TokenResponse
        {
            AccessToken = tokenResponse.AccessToken ?? string.Empty,
            RefreshToken = tokenResponse.RefreshToken ?? string.Empty,
            IdToken = tokenResponse.IdToken ?? string.Empty,
            TokenType = tokenResponse.TokenType ?? "Bearer",
            ExpiresIn = int.TryParse(tokenResponse.ExpiresIn, out var expiresIn) ? expiresIn : 3600,
            Scope = tokenResponse.Scope ?? string.Empty
        };

        await _tokenManagement.StoreTokensAsync(tokens);
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        var accessToken = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            return null;
        }

        return ExtractUserInfoFromToken(accessToken);
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        return await _tokenManagement.GetAccessTokenAsync();
    }

    public async Task SignOutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            // Clear local authentication
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        // Clear stored tokens
        await _tokenManagement.ClearTokensAsync();

        // Clear ReportServer session
        await _reportServerAuth.ClearSessionAsync();

        _logger.LogInformation("User signed out successfully");
    }

    private UserInfo? ExtractUserInfoFromToken(string accessToken)
    {
        try
        {
            if (!_jwtHandler.CanReadToken(accessToken))
            {
                return null;
            }

            var jwt = _jwtHandler.ReadJwtToken(accessToken);
            
            return new UserInfo
            {
                Subject = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty,
                Name = jwt.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value ?? string.Empty,
                Email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty,
                GivenName = jwt.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? string.Empty,
                FamilyName = jwt.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? string.Empty,
                EmailVerified = bool.TryParse(jwt.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value, out var emailVerified) && emailVerified,
                Roles = jwt.Claims.Where(c => c.Type == "realm_access" || c.Type == "roles")
                    .SelectMany(c => JsonSerializer.Deserialize<string[]>(c.Value) ?? [])
                    .ToArray(),
                Groups = jwt.Claims.Where(c => c.Type == "groups")
                    .SelectMany(c => JsonSerializer.Deserialize<string[]>(c.Value) ?? [])
                    .ToArray(),
                // Attributes = jwt.Claims.Where(c => !IsStandardClaim(c.Type))
                //     .ToDictionary(c => c.Type, c => (object)c.Value)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user info from token");
            return null;
        }
    }

    private IEnumerable<Claim> CreateClaimsFromToken(string accessToken)
    {
        var claims = new List<Claim>();
        
        try
        {
            if (_jwtHandler.CanReadToken(accessToken))
            {
                var jwt = _jwtHandler.ReadJwtToken(accessToken);
                
                // Add all claims from the token
                claims.AddRange(jwt.Claims);
                
                // Add standard identity claims
                var username = jwt.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
                if (!string.IsNullOrEmpty(username))
                {
                    claims.Add(new Claim(ClaimTypes.Name, username));
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? username));
                }

                var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                if (!string.IsNullOrEmpty(email))
                {
                    claims.Add(new Claim(ClaimTypes.Email, email));
                }

                // Add roles
                var roles = jwt.Claims.Where(c => c.Type == "realm_access" || c.Type == "roles")
                    .SelectMany(c => JsonSerializer.Deserialize<string[]>(c.Value) ?? []);
                
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating claims from token");
        }

        return claims;
    }

    private static bool IsStandardClaim(string claimType)
    {
        var standardClaims = new[]
        {
            "sub", "iss", "aud", "exp", "iat", "auth_time", "nonce", "acr", "amr", "azp",
            "preferred_username", "email", "email_verified", "given_name", "family_name",
            "name", "roles", "realm_access", "groups", "scope"
        };

        return standardClaims.Contains(claimType);
    }
}
