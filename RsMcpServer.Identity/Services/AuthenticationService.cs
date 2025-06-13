using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RsMcpServer.Identity.Models.Results;
using RsMcpServer.Identity.Models.Users;
using RsMcpServer.Identity.Models.Authentication;
using RsMcpServer.Identity.Models.Options;
using AuthenticationOptions = RsMcpServer.Identity.Models.Options.AuthenticationOptions;

namespace RsMcpServer.Identity.Services;

/// <summary>
/// Core authentication service that handles Keycloak integration
/// </summary>
public class AuthenticationService : ICustomAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakOptions _keycloakOptions;
    private readonly AuthenticationOptions _authOptions;
    private readonly JwtSecurityTokenHandler _jwtHandler;

    public AuthenticationService(
        ILogger<AuthenticationService> logger,
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        IOptions<KeycloakOptions> keycloakOptions,
        IOptions<AuthenticationOptions> authOptions)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _httpClientFactory = httpClientFactory;
        _keycloakOptions = keycloakOptions.Value;
        _authOptions = authOptions.Value;
        _jwtHandler = new JwtSecurityTokenHandler();
    }

    /// <summary>
    /// Authenticate user with username and password
    /// </summary>
    public async Task<AuthenticationResult> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting authentication for user: {Username}", request.Username);

            var httpClient = _httpClientFactory.CreateClient("keycloak");
            
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
                _logger.LogWarning("Authentication failed for user {Username}: {Error}", request.Username, errorContent);
                
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
                    Message = "Failed to parse token response"
                };
            }

            // Get user information
            var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken, cancellationToken);
            if (userInfo == null)
            {
                return new AuthenticationResult
                {
                    Success = false,
                    Message = "Failed to retrieve user information"
                };
            }

            // Create session and cookies
            var sessionId = await CreateSessionAsync(tokenResponse, userInfo, request.RememberMe);

            _logger.LogInformation("Successfully authenticated user: {Username}", request.Username);

            return new AuthenticationResult
            {
                Success = true,
                Message = "Authentication successful",
                User = userInfo,
                Tokens = tokenResponse,
                SessionId = sessionId,
                Claims = ExtractClaimsFromToken(tokenResponse.AccessToken)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for user: {Username}", request.Username);
            return new AuthenticationResult
            {
                Success = false,
                Message = "An error occurred during authentication"
            };
        }
    }

    /// <summary>
    /// Refresh authentication tokens
    /// </summary>
    public async Task<AuthenticationResult> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return new AuthenticationResult
                {
                    Success = false,
                    Message = "No HTTP context available"
                };
            }

            var refreshToken = httpContext.Session.GetString("refresh_token");
            if (string.IsNullOrEmpty(refreshToken))
            {
                return new AuthenticationResult
                {
                    Success = false,
                    Message = "No refresh token available"
                };
            }

            var httpClient = _httpClientFactory.CreateClient("keycloak");
            
            var tokenRequest = new Dictionary<string, string>
            {
                ["client_id"] = _keycloakOptions.ClientId,
                ["client_secret"] = _keycloakOptions.ClientSecret,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            };

            var content = new FormUrlEncodedContent(tokenRequest);
            var response = await httpClient.PostAsync(_keycloakOptions.TokenEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token refresh failed");
                return new AuthenticationResult
                {
                    Success = false,
                    Message = "Token refresh failed"
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
                    Message = "Failed to parse token response"
                };
            }

            // Update session with new tokens
            await UpdateSessionAsync(tokenResponse);

            _logger.LogInformation("Successfully refreshed tokens");

            return new AuthenticationResult
            {
                Success = true,
                Message = "Tokens refreshed successfully",
                Tokens = tokenResponse
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return new AuthenticationResult
            {
                Success = false,
                Message = "An error occurred during token refresh"
            };
        }
    }

    /// <summary>
    /// Validate current session
    /// </summary>
    public async Task<SessionValidationResult> ValidateSessionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null || !httpContext.User.Identity?.IsAuthenticated == true)
            {
                return new SessionValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "No authenticated user"
                };
            }

            var accessToken = httpContext.Session.GetString("access_token");
            if (string.IsNullOrEmpty(accessToken))
            {
                return new SessionValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "No access token in session"
                };
            }

            // Check if token is expired
            if (_jwtHandler.CanReadToken(accessToken))
            {
                var jwt = _jwtHandler.ReadJwtToken(accessToken);
                if (jwt.ValidTo < DateTime.UtcNow)
                {
                    return new SessionValidationResult
                    {
                        IsValid = false,
                        RequiresRefresh = true,
                        ErrorMessage = "Access token expired"
                    };
                }
            }

            // Optionally validate with Keycloak user info endpoint
            var userInfo = await GetUserInfoAsync(accessToken, cancellationToken);
            if (userInfo == null)
            {
                return new SessionValidationResult
                {
                    IsValid = false,
                    RequiresRefresh = true,
                    ErrorMessage = "Failed to validate token with Keycloak"
                };
            }

            return new SessionValidationResult
            {
                IsValid = true,
                User = userInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during session validation");
            return new SessionValidationResult
            {
                IsValid = false,
                ErrorMessage = "Session validation error"
            };
        }
    }

    /// <summary>
    /// Logout user
    /// </summary>
    public async Task<bool> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return false;
            }

            // Get tokens for Keycloak logout
            var refreshToken = httpContext.Session.GetString("refresh_token");
            
            // Clear local session
            httpContext.Session.Clear();
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Logout from Keycloak if requested
            if (request.LogoutFromKeycloak && !string.IsNullOrEmpty(refreshToken))
            {
                try
                {
                    var httpClient = _httpClientFactory.CreateClient("keycloak");
                    
                    var logoutRequest = new Dictionary<string, string>
                    {
                        ["client_id"] = _keycloakOptions.ClientId,
                        ["client_secret"] = _keycloakOptions.ClientSecret,
                        ["refresh_token"] = refreshToken
                    };

                    var content = new FormUrlEncodedContent(logoutRequest);
                    await httpClient.PostAsync(_keycloakOptions.LogoutEndpoint, content, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to logout from Keycloak, but local logout succeeded");
                }
            }

            _logger.LogInformation("User logged out successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return false;
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    public async Task<UserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        var accessToken = httpContext.Session.GetString("access_token");
        if (string.IsNullOrEmpty(accessToken))
        {
            return null;
        }

        return await GetUserInfoAsync(accessToken, cancellationToken);
    }

    /// <summary>
    /// Get access token for API calls
    /// </summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        var accessToken = httpContext.Session.GetString("access_token");
        
        // Check if token needs refresh
        if (!string.IsNullOrEmpty(accessToken) && _jwtHandler.CanReadToken(accessToken))
        {
            var jwt = _jwtHandler.ReadJwtToken(accessToken);
            if (jwt.ValidTo < DateTime.UtcNow.AddMinutes(5)) // Refresh if expires within 5 minutes
            {
                var refreshResult = await RefreshTokenAsync();
                if (refreshResult.Success && refreshResult.Tokens != null)
                {
                    return refreshResult.Tokens.AccessToken;
                }
            }
        }

        return accessToken;
    }

    /// <summary>
    /// Get user information from Keycloak
    /// </summary>
    private async Task<UserInfo?> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("keycloak");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.GetAsync(_keycloakOptions.UserInfoEndpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var userInfoJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<UserInfo>(userInfoJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user info");
            return null;
        }
    }

    /// <summary>
    /// Create authentication session
    /// </summary>
    private async Task<string> CreateSessionAsync(TokenResponse tokens, UserInfo userInfo, bool isPersistent)
    {
        var httpContext = _httpContextAccessor.HttpContext!;
        
        // Create claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userInfo.Subject),
            new(ClaimTypes.Name, userInfo.PreferredUsername),
            new(ClaimTypes.GivenName, userInfo.GivenName),
            new(ClaimTypes.Surname, userInfo.FamilyName),
            new(ClaimTypes.Email, userInfo.Email),
            new("preferred_username", userInfo.PreferredUsername),
            new("email_verified", userInfo.EmailVerified.ToString())
        };

        // Add group claims
        foreach (var group in userInfo.Groups)
        {
            claims.Add(new Claim("groups", group));
        }

        // Add role claims
        foreach (var role in userInfo.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = isPersistent,
            ExpiresUtc = tokens.ExpiresAt,
            AllowRefresh = true
        };

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

        // Store tokens in session
        httpContext.Session.SetString("access_token", tokens.AccessToken);
        httpContext.Session.SetString("refresh_token", tokens.RefreshToken);
        httpContext.Session.SetString("id_token", tokens.IdToken);

        var sessionId = Guid.NewGuid().ToString();
        httpContext.Session.SetString("session_id", sessionId);

        return sessionId;
    }

    /// <summary>
    /// Update session with new tokens
    /// </summary>
    private async Task UpdateSessionAsync(TokenResponse tokens)
    {
        var httpContext = _httpContextAccessor.HttpContext!;
        
        // Update session tokens
        httpContext.Session.SetString("access_token", tokens.AccessToken);
        httpContext.Session.SetString("refresh_token", tokens.RefreshToken);
        if (!string.IsNullOrEmpty(tokens.IdToken))
        {
            httpContext.Session.SetString("id_token", tokens.IdToken);
        }

        // Update authentication properties
        var authResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (authResult.Succeeded)
        {
            var properties = authResult.Properties;
            properties.ExpiresUtc = tokens.ExpiresAt;
            
            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, authResult.Principal, properties);
        }
    }

    /// <summary>
    /// Extract claims from JWT token
    /// </summary>
    private IDictionary<string, object> ExtractClaimsFromToken(string token)
    {
        var claims = new Dictionary<string, object>();
        
        if (_jwtHandler.CanReadToken(token))
        {
            var jwt = _jwtHandler.ReadJwtToken(token);
            foreach (var claim in jwt.Claims)
            {
                claims[claim.Type] = claim.Value;
            }
        }

        return claims;
    }
}
