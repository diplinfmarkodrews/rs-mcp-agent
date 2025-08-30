using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.Logging;
using ReportServer.RestClient.DTOs.Authentication;
using ReportServer.RestClient.Infrastructure;
using ReportServer.RestClient.DTOs;

namespace ReportServer.RestClient.Services;

public class RsRestAuthenticationClient : RsRestClientBase
{
    private readonly ILogger _logger;

    // Constructor
    public RsRestAuthenticationClient(ILogger logger, IHttpClientFactory httpClientFactory, CookieContainerProvider cookieProvider)
        : base(httpClient, cookieProvider)
    {
        _logger = logger;
    }

    // Authentication
    public async Task<RestResponse<AuthenticationResultDto>> AuthenticateAsync(string username, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", new AuthenticationRequestDto
            {
                Username = username,
                Password = password
            });
            response.EnsureSuccessStatusCode();
            var authResult = await response.Content.ReadFromJsonAsync<AuthenticationResultDto>();
            if (authResult != null && response.IsSuccessStatusCode)
            {
                return RestResponse<AuthenticationResultDto>.Successful(authResult);
            }
            
            return new RestResponse<AuthenticationResultDto>
            {
                Success = false,
                Error = authResult?.ErrorMessage ?? "Authentication failed",
                StatusCode = response.StatusCode != null ? (int)response.StatusCode : null
            };
        }
        catch (Exception e) 
        {
            return new RestResponse<AuthenticationResultDto>()
        }
        
    }

    private AuthenticationResultDto ParseAuthenticationResponse(string response)
    {
        JsonSerializer.Deserialize(response)

        if (gwtResponse.StartsWith("//OK"))
        {
            // Parse successful response
            // Extract session from cookies
            var token = ExtractTokenFromCookies();

            // Parse user data from GWT response
            var userData = ParseUserDataFromGwtResponse(Response);

            return new AuthenticationResultDto
            {
                Success = true,
                Token = token,
                User = userData
            };
        }

        return new AuthenticationResultDto
        {
            Success = false,
            ErrorMessage = "Invalid response format"
        };
    }

    private string ExtractTokenFromCookies()
    {
        var cookies = _cookieContainer.GetCookies(_httpClient.BaseAddress);
        var sessionCookie = cookies["JSESSIONID"];
        return sessionCookie?.Value;
    }

   

    public async Task<RestResponse<string>> LogoutAsync()
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/logout", new { });
            var responseString = await response.Content.ReadFromJsonAsync<string>();
            
            return RestResponse<string>.Successful(responseString, "Logout successful");
        }
        catch (Exception ex)
        {
            return new RestResponse<string>
            {
                Success = false,
                Error = $"Logout error: {ex.Message}",
                Exception = ex
            };
        }
    }
}
