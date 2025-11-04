namespace RSChatApp.Web.Services.Authentication;

public interface IAuthenticationClient
{
    Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken);
}
public class AuthenticationClient : IAuthenticationClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthenticationClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        using var httpClient = _httpClientFactory.CreateClient("RsMcpServer");
        var response = await httpClient.PostAsJsonAsync("api/auth/v1/login", new { username, password }, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var authResult = await response.Content.ReadFromJsonAsync<AuthenticationResult>();
            return authResult ?? new AuthenticationResult { Success = false, Error = "Invalid response from server" };
        }
        return new AuthenticationResult { Success = false, Error = $"Authentication failed: {response.ReasonPhrase}" };
    }
}
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

