using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using RSChatApp.Infrastructure.Identity.Models;

namespace RSChatApp.Web.Services.Authentication;

/// <summary>
/// Service for managing authentication state and user information
/// </summary>
public interface IAuthenticationInfoService
{
    /// <summary>
    /// Gets the current authentication state
    /// </summary>
    Task<AuthenticationInfo> GetAuthenticationInfoAsync();
    
    /// <summary>
    /// Event raised when authentication state changes
    /// </summary>
    event EventHandler<AuthenticationInfo> AuthenticationStateChanged;
    
    /// <summary>
    /// Refreshes the authentication state from the server
    /// </summary>
    Task RefreshAuthenticationStateAsync();
}



/// <summary>
/// Implementation of authentication service that wraps ASP.NET Core authentication
/// </summary>
public class AuthenticationInfoService : IAuthenticationInfoService, IDisposable
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<AuthenticationInfoService> _logger;
    
    public event EventHandler<AuthenticationInfo>? AuthenticationStateChanged;

    public AuthenticationInfoService(
        AuthenticationStateProvider authenticationStateProvider,
        IJSRuntime jsRuntime,
        ILogger<AuthenticationInfoService> logger)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _jsRuntime = jsRuntime;
        _logger = logger;
        
        // Subscribe to authentication state changes
        _authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    public async Task<AuthenticationInfo> GetAuthenticationInfoAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return CreateAuthenticationInfo(authState);
    }
    
    public async Task RefreshAuthenticationStateAsync()
    {
        _logger.LogInformation("Refreshing authentication state for widget update");
        
        // Wait for cookie to be set in the HTTP context
        await Task.Delay(500);
        
        var authInfo = await GetAuthenticationInfoAsync();
        
        if (authInfo.IsAuthenticated)
        {
            _logger.LogInformation("User authenticated - IsAuthenticated: {IsAuthenticated}, User: {UserName}", 
                authInfo.IsAuthenticated, authInfo.UserName);
            
            // Notify subscribers (like AuthenticationWidget)
            AuthenticationStateChanged?.Invoke(this, authInfo);
            return;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            _logger.LogInformation("Initiating user logout");
            
            // Use JavaScript to perform a POST request to the logout endpoint
            await _jsRuntime.InvokeVoidAsync("eval", @"
                const form = document.createElement('form');
                form.method = 'POST';
                form.action = '/auth/logout';
                document.body.appendChild(form);
                form.submit();
            ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            throw;
        }
    }

    private async void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        try
        {
            var authState = await task;
            var authInfo = CreateAuthenticationInfo(authState);
            AuthenticationStateChanged?.Invoke(this, authInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling authentication state change");
        }
    }

    private static AuthenticationInfo CreateAuthenticationInfo(AuthenticationState authState)
    {
        var isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
        
        if (!isAuthenticated)
        {
            return new AuthenticationInfo
            {
                IsAuthenticated = false,
                User = authState.User
            };
        }

        var userName = authState.User.Identity?.Name ?? 
                      authState.User.FindFirst("preferred_username")?.Value ?? 
                      authState.User.FindFirst("name")?.Value ?? 
                      "User";

        var displayName = authState.User.FindFirst("given_name")?.Value ?? 
                         authState.User.FindFirst("name")?.Value ?? 
                         userName;

        var email = authState.User.FindFirst("email")?.Value ?? "";

        var roles = authState.User.FindAll("role")
                          .Concat(authState.User.FindAll(ClaimTypes.Role))
                          .Select(c => c.Value)
                          .Distinct()
                          .ToList();
        
        return new AuthenticationInfo
        {
            IsAuthenticated = true,
            UserName = userName,
            DisplayName = displayName,
            Email = email,
            Roles = roles,
            User = authState.User
        };
    }

    public void Dispose()
    {
        _authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}
