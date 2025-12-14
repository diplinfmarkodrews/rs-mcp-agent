using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using RSChatApp.Infrastructure.ReportServer.Terminal;

namespace RSChatApp.Infrastructure.Identity.Clients;

public interface IAuthenticationClient
{
    Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken);
    Task<RefreshResponse> RefreshTokenAsync(string token, CancellationToken cancellationToken);
    Task<LogoutResponse> LogoutAsync(string token, CancellationToken cancellationToken);
}
public class LegacyAuthenticationClient : IAuthenticationClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LegacyAuthenticationClient> _logger;

    public LegacyAuthenticationClient(IHttpClientFactory httpClientFactory, ILogger<LegacyAuthenticationClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Authenticating user {Username} via LegacyAuthenticationClient", username);
        using var httpClient = _httpClientFactory.CreateClient(RsMcpServerHttpClientName.ClientName);
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/auth/v1/login", new { username, password }, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AuthenticationResult>(cancellationToken)??
                   new AuthenticationResult { Success = false, Error = "Invalid response from authentication server" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating user {Username} via LegacyAuthenticationClient, reason: {Reason}", username, ex.Message);
            return new AuthenticationResult { Success = false, Error = $"Authentication failed: {ex.Message}" };
        }
    }
    
    public async Task<RefreshResponse> RefreshTokenAsync(string token, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing token via LegacyAuthenticationClient");
        using var httpClient = _httpClientFactory.CreateClient(RsMcpServerHttpClientName.ClientName);
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/auth/v1/refresh-token", new { token }, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RefreshResponse>(cancellationToken) ??
                   new RefreshResponse(Success: false, Error: "Invalid response from authentication server", Token: null, ExpiresAt: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token via LegacyAuthenticationClient, reason: {Reason}", ex.Message);
            return new RefreshResponse(Success: false, Error: $"Token refresh failed: {ex.Message}", Token: null, ExpiresAt: null);
        }
    }
    
    public async Task<LogoutResponse> LogoutAsync(string token, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Logging out via LegacyAuthenticationClient");
        using var httpClient = _httpClientFactory.CreateClient(RsMcpServerHttpClientName.ClientName);
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/auth/v1/logout", new { token }, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<LogoutResponse>(cancellationToken) ??
                   new LogoutResponse(Success: false, Error: "Invalid response from authentication server");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging out via LegacyAuthenticationClient, reason: {Reason}", ex.Message);
            return new LogoutResponse(Success: false, Error: $"Logout failed: {ex.Message}");
        }
    }
}
public record RefreshResponse(
    bool Success, 
    string? Token, 
    DateTime? ExpiresAt, 
    string? Error);

/// <summary>
/// Response model for logout operations
/// </summary>
public record LogoutResponse(bool Success, string? Error);
public record AuthenticationResult
{
    public bool Success { get; init; }
    public string Token { get; init; }
    
    public UserDto User { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string Error { get; init; }
}
public record UserDto
{
    public string Id { get; init; }
    public string Username { get; init; }
    public string Email { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public List<string> Roles { get; init; }
    public Dictionary<string, string> Properties { get; init; }
}

