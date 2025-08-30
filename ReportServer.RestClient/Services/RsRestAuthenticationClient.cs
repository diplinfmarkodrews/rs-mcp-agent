using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ReportServer.RestClient.DTOs.Authentication;
using ReportServer.RestClient.Infrastructure;
using ReportServer.RestClient.DTOs;

namespace ReportServer.RestClient.Services;

public class RsRestAuthenticationClient : RsRestClientBase
{
    private readonly ILogger _logger;

    public RsRestAuthenticationClient(ILogger logger, IHttpClientFactory httpClientFactory, CookieContainerProvider cookieProvider)
        : base(httpClientFactory, cookieProvider)
    {
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user with ReportServer
    /// Based on traced request: authenticate with username/password
    /// </summary>
    public async Task<RestResponse<AuthenticationResultDto>> AuthenticateAsync(string username, string password)
    {
        try
        {
            var request = new AuthenticationRequestDto
            {
                Username = username,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            var authResult = await response.Content.ReadFromJsonAsync<AuthenticationResultDto>();
            
            if (response.IsSuccessStatusCode && authResult != null && authResult.Success)
            {
                _logger.LogInformation("Authentication successful for user: {Username}", username);
                return RestResponse<AuthenticationResultDto>.Successful(authResult);
            }
            
            _logger.LogWarning("Authentication failed for user: {Username}", username);
            return new RestResponse<AuthenticationResultDto>
            {
                Success = false,
                Error = authResult?.ErrorMessage ?? "Authentication failed",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Authentication error for user: {Username}", username);
            return new RestResponse<AuthenticationResultDto>
            {
                Success = false,
                Error = $"Authentication error: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Check if user session is still valid
    /// Based on traced request: isAuthenticated
    /// </summary>
    public async Task<RestResponse<AuthenticationResultDto>> IsAuthenticatedAsync(string? sessionId = null)
    {
        try
        {
            var url = "api/auth/check";
            if (!string.IsNullOrEmpty(sessionId))
            {
                url += $"?sessionId={sessionId}";
            }

            var response = await _httpClient.GetAsync(url);
            var authResult = await response.Content.ReadFromJsonAsync<AuthenticationResultDto>();
            
            if (response.IsSuccessStatusCode && authResult != null)
            {
                return RestResponse<AuthenticationResultDto>.Successful(authResult);
            }
            
            return new RestResponse<AuthenticationResultDto>
            {
                Success = false,
                Error = authResult?.ErrorMessage ?? "Session check failed",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session check error");
            return new RestResponse<AuthenticationResultDto>
            {
                Success = false,
                Error = $"Session check error: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Get HMAC challenge passphrase for password security
    /// Based on traced request: getHmacPassphrase
    /// </summary>
    public async Task<RestResponse<string>> GetHmacPassphraseAsync(string? sessionId = null)
    {
        try
        {
            var url = "api/auth/challenge";
            if (!string.IsNullOrEmpty(sessionId))
            {
                url += $"?sessionId={sessionId}";
            }

            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var passphrase = await response.Content.ReadAsStringAsync();
                return RestResponse<string>.Successful(passphrase.Trim('"')); // Remove JSON quotes
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return new RestResponse<string>
            {
                Success = false,
                Error = errorContent ?? "Failed to get HMAC passphrase",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting HMAC passphrase");
            return new RestResponse<string>
            {
                Success = false,
                Error = $"HMAC passphrase error: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Logout user from ReportServer
    /// </summary>
    public async Task<RestResponse<string>> LogoutAsync(string? sessionId = null)
    {
        try
        {
            var url = "api/auth/logout";
            if (!string.IsNullOrEmpty(sessionId))
            {
                url += $"?sessionId={sessionId}";
            }

            var response = await _httpClient.PostAsync(url, null);
            var responseString = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Logout successful");
                return RestResponse<string>.Successful(responseString, (int)response.StatusCode);
            }
            
            return new RestResponse<string>
            {
                Success = false,
                Error = responseString ?? "Logout failed",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error");
            return new RestResponse<string>
            {
                Success = false,
                Error = $"Logout error: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Health check for the authentication service
    /// </summary>
    public async Task<RestResponse<string>> HealthCheckAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/health");
            var healthStatus = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return RestResponse<string>.Successful(healthStatus);
            }
            
            return new RestResponse<string>
            {
                Success = false,
                Error = "Health check failed",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check error");
            return new RestResponse<string>
            {
                Success = false,
                Error = $"Health check error: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Extract session token from cookies
    /// </summary>
    private string? ExtractTokenFromCookies()
    {
        try
        {
            var cookies = _cookieContainer.GetCookies(_httpClient.BaseAddress!);
            var sessionCookie = cookies["JSESSIONID"];
            return sessionCookie?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract session token from cookies");
            return null;
        }
    }
}
