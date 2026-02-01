using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Keycloak.AuthServices.Authentication;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace RsMcpServer.Identity.Services;

public interface ITokenManagementService
{
    Task<TokenRefreshResult> RefreshTokenAsync(CancellationToken cancellationToken = default);
    Task<string?> GetAccessTokenAsync();
    Task StoreTokensFromContextAsync(TokenValidatedContext context);
    Task ClearTokensAsync();
}

public class TokenRefreshResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public TimeSpan? ExpiresIn { get; set; }
}

/// <summary>
/// Service for managing authentication tokens using Keycloak AuthServices
/// </summary>
public class TokenManagementService : ITokenManagementService
{
    private readonly ILogger<TokenManagementService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakAuthenticationOptions _keycloakOptions;

    private const string AccessTokenKey = "auth:access_token";
    private const string RefreshTokenKey = "auth:refresh_token";
    private const string IdTokenKey = "auth:id_token";
    private const string TokenExpiryKey = "auth:token_expiry";

    public TokenManagementService(
        ILogger<TokenManagementService> logger,
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        IOptions<KeycloakAuthenticationOptions> keycloakOptions)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _httpClientFactory = httpClientFactory;
        _keycloakOptions = keycloakOptions.Value;
    }

    public async Task<TokenRefreshResult> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var refreshToken = await GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken))
            {
                return new TokenRefreshResult
                {
                    Success = false,
                    Message = "No refresh token available"
                };
            }

            _logger.LogInformation("Refreshing access token");

            using var httpClient = _httpClientFactory.CreateClient();
            
            var tokenEndpoint = $"{_keycloakOptions.AuthServerUrl}/protocol/openid-connect/token";
            
            var requestData = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = _keycloakOptions.Resource,
            };

            if (!string.IsNullOrEmpty(_keycloakOptions.Credentials?.Secret))
            {
                requestData["client_secret"] = _keycloakOptions.Credentials.Secret;
            }

            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(requestData)
            };

            var response = await httpClient.SendAsync(request, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);
                
                var accessToken = tokenResponse.GetProperty("access_token").GetString();
                var newRefreshToken = tokenResponse.TryGetProperty("refresh_token", out var refreshProp) 
                    ? refreshProp.GetString() 
                    : refreshToken;
                
                var expiresIn = tokenResponse.TryGetProperty("expires_in", out var expiresProp)
                    ? TimeSpan.FromSeconds(expiresProp.GetInt32())
                    : TimeSpan.FromHours(1);

                // Store new tokens
                await StoreTokenAsync(AccessTokenKey, accessToken);
                await StoreTokenAsync(RefreshTokenKey, newRefreshToken);
                await StoreTokenAsync(TokenExpiryKey, DateTimeOffset.UtcNow.Add(expiresIn).ToString("O"));

                _logger.LogInformation("Access token refreshed successfully");

                return new TokenRefreshResult
                {
                    Success = true,
                    ExpiresIn = expiresIn
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Token refresh failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                
                return new TokenRefreshResult
                {
                    Success = false,
                    Message = $"Token refresh failed: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return new TokenRefreshResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        var token = await GetTokenAsync(AccessTokenKey);
        
        if (string.IsNullOrEmpty(token))
            return null;

        // Check if token is expired
        var expiryString = await GetTokenAsync(TokenExpiryKey);
        if (!string.IsNullOrEmpty(expiryString) && 
            DateTimeOffset.TryParse(expiryString, out var expiry) && 
            expiry <= DateTimeOffset.UtcNow.AddMinutes(-5)) // 5 minute buffer
        {
            _logger.LogInformation("Access token expired, attempting refresh");
            var refreshResult = await RefreshTokenAsync();
            if (refreshResult.Success)
            {
                return await GetTokenAsync(AccessTokenKey);
            }
            return null;
        }

        return token;
    }

    public async Task StoreTokensFromContextAsync(TokenValidatedContext context)
    {
        try
        {
            var properties = context.Properties;
            
            if (properties.GetTokenValue("access_token") is string accessToken)
            {
                await StoreTokenAsync(AccessTokenKey, accessToken);
            }

            if (properties.GetTokenValue("refresh_token") is string refreshToken)
            {
                await StoreTokenAsync(RefreshTokenKey, refreshToken);
            }

            if (properties.GetTokenValue("id_token") is string idToken)
            {
                await StoreTokenAsync(IdTokenKey, idToken);
            }

            // Calculate expiry
            if (properties.GetTokenValue("expires_at") is string expiresAt)
            {
                await StoreTokenAsync(TokenExpiryKey, expiresAt);
            }
            else if (properties.GetTokenValue("expires_in") is string expiresInStr &&
                     int.TryParse(expiresInStr, out var expiresInSeconds))
            {
                var expiry = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);
                await StoreTokenAsync(TokenExpiryKey, expiry.ToString("O"));
            }

            _logger.LogInformation("Tokens stored successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing tokens from context");
        }
    }

    public async Task ClearTokensAsync()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session != null)
        {
            session.Remove(AccessTokenKey);
            session.Remove(RefreshTokenKey);
            session.Remove(IdTokenKey);
            session.Remove(TokenExpiryKey);
            await session.CommitAsync();
        }
    }

    private async Task<string?> GetRefreshTokenAsync()
    {
        return await GetTokenAsync(RefreshTokenKey);
    }

    private async Task<string?> GetTokenAsync(string key)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null) return null;

        await session.LoadAsync();
        return session.GetString(key);
    }

    private async Task StoreTokenAsync(string key, string? value)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null) return;

        await session.LoadAsync();
        if (string.IsNullOrEmpty(value))
        {
            session.Remove(key);
        }
        else
        {
            session.SetString(key, value);
        }
        await session.CommitAsync();
    }
}
