using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RsMcpServer.Identity.Models.Options;
using RsMcpServer.Identity.Models.Results;

namespace RsMcpServer.Identity.Services;

/// <summary>
/// Service for managing authentication tokens
/// </summary>
public class TokenManagementService : ITokenManagementService
{
    private readonly ILogger<TokenManagementService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakOptions _keycloakOptions;

    private const string AccessTokenKey = "auth:access_token";
    private const string RefreshTokenKey = "auth:refresh_token";
    private const string IdTokenKey = "auth:id_token";
    private const string TokenExpiryKey = "auth:token_expiry";

    public TokenManagementService(
        ILogger<TokenManagementService> logger,
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        IOptions<KeycloakOptions> keycloakOptions)
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

            using var httpClient = _httpClientFactory.CreateClient("keycloak");
            
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
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Token refresh failed: {Error}", errorContent);
                
                return new TokenRefreshResult
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
                return new TokenRefreshResult
                {
                    Success = false,
                    Message = "Invalid token response"
                };
            }

            // Store the new tokens
            await StoreTokensAsync(tokenResponse);

            _logger.LogInformation("Access token refreshed successfully");

            return new TokenRefreshResult
            {
                Success = true,
                Message = "Token refreshed successfully",
                ExpiresIn = tokenResponse.ExpiresIn,
                TokenResponse = tokenResponse
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            
            return new TokenRefreshResult
            {
                Success = false,
                Message = "Token refresh error"
            };
        }
    }

    public async Task<bool> TokenNeedsRefreshAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Session == null)
        {
            return false;
        }

        var expiryString = httpContext.Session.GetString(TokenExpiryKey);
        if (string.IsNullOrEmpty(expiryString) || 
            !DateTimeOffset.TryParse(expiryString, out var expiry))
        {
            return true; // If we can't determine expiry, assume refresh is needed
        }

        // Check if token expires within the threshold
        return DateTimeOffset.UtcNow.Add(_keycloakOptions.TokenRefreshThreshold) >= expiry;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Session == null)
        {
            return null;
        }

        // Check if token needs refresh first
        if (await TokenNeedsRefreshAsync())
        {
            var refreshResult = await RefreshTokenAsync();
            if (!refreshResult.Success)
            {
                _logger.LogWarning("Failed to refresh token when getting access token");
                return null;
            }
        }

        return httpContext.Session.GetString(AccessTokenKey);
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        await Task.CompletedTask; // For async consistency
        
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.Session?.GetString(RefreshTokenKey);
    }

    public async Task StoreTokensAsync(TokenResponse tokenResponse)
    {
        await Task.CompletedTask; // For async consistency
        
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Session == null)
        {
            _logger.LogWarning("Cannot store tokens: HttpContext or Session is null");
            return;
        }

        try
        {
            // Store tokens in session
            httpContext.Session.SetString(AccessTokenKey, tokenResponse.AccessToken);
            
            if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
            {
                httpContext.Session.SetString(RefreshTokenKey, tokenResponse.RefreshToken);
            }
            
            if (!string.IsNullOrEmpty(tokenResponse.IdToken))
            {
                httpContext.Session.SetString(IdTokenKey, tokenResponse.IdToken);
            }

            // Calculate and store expiry time
            var expiry = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
            httpContext.Session.SetString(TokenExpiryKey, expiry.ToString("O"));

            _logger.LogInformation("Tokens stored successfully, expires at: {Expiry}", expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing tokens");
        }
    }

    public async Task ClearTokensAsync()
    {
        await Task.CompletedTask; // For async consistency
        
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Session == null)
        {
            return;
        }

        try
        {
            httpContext.Session.Remove(AccessTokenKey);
            httpContext.Session.Remove(RefreshTokenKey);
            httpContext.Session.Remove(IdTokenKey);
            httpContext.Session.Remove(TokenExpiryKey);

            _logger.LogInformation("Tokens cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing tokens");
        }
    }
}
