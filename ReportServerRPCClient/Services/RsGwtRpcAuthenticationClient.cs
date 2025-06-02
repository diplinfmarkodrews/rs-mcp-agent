using System.Net;
using AutoMapper;
using ReportServerRPCClient.DTOs;
using ReportServerRPCClient.DTOs.Authentication;
using ReportServerRPCClient.Infrastructure;

namespace ReportServerRPCClient.Services;

public class RsGwtRpcAuthenticationClient : ReportServerGwtRpcClientBase
{
    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookieContainer;
    
    // Constructor
    public RsGwtRpcAuthenticationClient(HttpClient httpClient, CookieContainerProvider cookieProvider)
        : base(httpClient, cookieProvider)
    {
        _httpClient = httpClient;
        _cookieContainer = cookieProvider.CookieContainer;

        if (_httpClient.BaseAddress is null)
            throw new InvalidOperationException("BaseAddress not set in HTTP client.");
    }

    // Authentication
    public async Task<GwtRpcResponse<AuthenticationResultDto>> AuthenticateAsync(string username, string password)
    {
        var payload = BuildGwtRpcPayload(
            "net.datenwerke.security.client.security.rpc.SecurityRpcService",
            "authenticate",
            username, password
        );
        var response = await PostGwtRpcAsync("security", payload);
        var parsedResult = ParseAuthenticationResponse(response);
        if (parsedResult.Success)
        {
            return GwtRpcResponse<AuthenticationResultDto>.Successful(response, parsedResult);
        }
        return new GwtRpcResponse<AuthenticationResultDto>
        {
            Success = false,
            Error = parsedResult.ErrorMessage,
            Exception = new Exception(parsedResult.ErrorMessage)
        };
    }

    private AuthenticationResultDto ParseAuthenticationResponse(string gwtResponse)
    {
        if (gwtResponse.StartsWith("//EX"))
        {
            // Parse exception response
            var errorMatch = System.Text.RegularExpressions.Regex.Match(
                gwtResponse, @"\[""([^""]+)""\]$");

            return new AuthenticationResultDto
            {
                Success = false,
                ErrorMessage = errorMatch.Success ? errorMatch.Groups[1].Value : "Authentication failed"
            };
        }

        if (gwtResponse.StartsWith("//OK"))
        {
            // Parse successful response
            // Extract session from cookies
            var sessionId = ExtractSessionFromCookies();

            // Parse user data from GWT response
            var userData = ParseUserDataFromGwtResponse(gwtResponse);

            return new AuthenticationResultDto
            {
                Success = true,
                SessionId = sessionId,
                User = userData
            };
        }

        return new AuthenticationResultDto
        {
            Success = false,
            ErrorMessage = "Invalid response format"
        };
    }

    private string ExtractSessionFromCookies()
    {
        var cookies = _cookieContainer.GetCookies(_httpClient.BaseAddress);
        var sessionCookie = cookies["JSESSIONID"];
        return sessionCookie?.Value;
    }

    private UserDto ParseUserDataFromGwtResponse(string gwtResponse)
    {
        // Parse the GWT serialized response
        // This is a simplified version - actual parsing would be more complex
        var dataPattern = @"\[([^\]]+)\]$";
        var match = System.Text.RegularExpressions.Regex.Match(gwtResponse, dataPattern);

        if (match.Success)
        {
            var dataArray = match.Groups[1].Value.Split(',');
            return new UserDto
            {
                Username = dataArray[0].Trim('"'),
                Email = dataArray.Length > 1 ? dataArray[1].Trim('"') : null,
                Id = dataArray.Length > 2 && long.TryParse(dataArray[2], out var id) ? id : 0
            };
        }

        return null;
    }
}
