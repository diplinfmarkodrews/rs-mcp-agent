# Example .NET Integration

This folder contains example code showing how to integrate with the Java ReportServer Sidecar from a .NET application.

## C# HTTP Client Example

```csharp
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ReportServerIntegration
{
    public class AuthenticationRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool Encrypted { get; set; } = false;
    }

    public class AuthenticationResponse
    {
        public bool Success { get; set; }
        public string SessionId { get; set; }
        public UserInfo User { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
    }

    public class UserInfo
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public bool Active { get; set; }
        public bool SuperUser { get; set; }
        public List<string> Groups { get; set; }
        public List<string> Roles { get; set; }
    }

    public class ReportServerSidecarClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private string _sessionId;

        public ReportServerSidecarClient(HttpClient httpClient, string baseUrl = "http://localhost:8091")
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                var request = new AuthenticationRequest
                {
                    Username = username,
                    Password = password
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/reportserver/auth/login", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonSerializer.Deserialize<AuthenticationResponse>(responseContent);

                    if (authResponse.Success)
                    {
                        _sessionId = authResponse.SessionId;
                        Console.WriteLine($"Logged in successfully as {authResponse.User.Username}");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"Login failed: {authResponse.Message}");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine($"HTTP Error: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during login: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CheckAuthenticationAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/reportserver/auth/check?sessionId={_sessionId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonSerializer.Deserialize<AuthenticationResponse>(responseContent);
                    return authResponse.Success;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during auth check: {ex.Message}");
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(_sessionId))
                {
                    await _httpClient.PostAsync($"{_baseUrl}/api/reportserver/auth/logout?sessionId={_sessionId}", null);
                    _sessionId = null;
                    Console.WriteLine("Logged out successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during logout: {ex.Message}");
            }
        }

        public async Task<bool> TestHealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/reportserver/health");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during health check: {ex.Message}");
                return false;
            }
        }
    }

    // Example usage in a console application
    class Program
    {
        static async Task Main(string[] args)
        {
            using var httpClient = new HttpClient();
            var client = new ReportServerSidecarClient(httpClient);

            // Test health
            Console.WriteLine("Testing sidecar health...");
            if (await client.TestHealthAsync())
            {
                Console.WriteLine("Sidecar is healthy");
            }
            else
            {
                Console.WriteLine("Sidecar is not responding");
                return;
            }

            // Login
            Console.WriteLine("Attempting login...");
            if (await client.LoginAsync("admin", "admin"))
            {
                Console.WriteLine("Login successful");

                // Check authentication
                Console.WriteLine("Checking authentication...");
                if (await client.CheckAuthenticationAsync())
                {
                    Console.WriteLine("Authentication is valid");
                }
                else
                {
                    Console.WriteLine("Authentication is invalid");
                }

                // Logout
                Console.WriteLine("Logging out...");
                await client.LogoutAsync();
            }
            else
            {
                Console.WriteLine("Login failed");
            }
        }
    }
}
```

## Running the Example

1. Start the ReportServer sidecar:
   ```bash
   cd java-rs-sidecar
   mvn spring-boot:run
   ```

2. Create a new .NET console application:
   ```bash
   dotnet new console -n ReportServerExample
   cd ReportServerExample
   ```

3. Copy the example code above into `Program.cs`

4. Run the .NET application:
   ```bash
   dotnet run
   ```

## Integration Patterns

### Dependency Injection in ASP.NET Core

```csharp
// In Startup.cs or Program.cs
services.AddHttpClient<ReportServerSidecarClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:8080");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// In your service or controller
public class MyService
{
    private readonly ReportServerSidecarClient _reportServerClient;

    public MyService(ReportServerSidecarClient reportServerClient)
    {
        _reportServerClient = reportServerClient;
    }

    public async Task<bool> AuthenticateUserAsync(string username, string password)
    {
        return await _reportServerClient.LoginAsync(username, password);
    }
}
```

### Session Management

Store the session ID in:
- HTTP Session (for web applications)
- Cache (Redis/MemoryCache)
- Database (for persistent sessions)
- JWT tokens (for stateless APIs)

### Error Handling

Implement retry policies and circuit breakers:

```csharp
services.AddHttpClient<ReportServerSidecarClient>()
    .AddPolicyHandler(GetRetryPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
