using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using RSChatApp.Web.Models.Authentication;

namespace RSChatApp.Web.Services.Authentication;

/// <summary>
/// Service for managing authentication state and user information
/// </summary>
public interface IAuthenticationService
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
    /// Triggers a logout request
    /// </summary>
    Task LogoutAsync();
    
}



/// <summary>
/// Implementation of authentication service that wraps ASP.NET Core authentication
/// </summary>
public class BlazorAuthenticationService : IAuthenticationService, IDisposable
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<BlazorAuthenticationService> _logger;
    
    public event EventHandler<AuthenticationInfo>? AuthenticationStateChanged;

    public BlazorAuthenticationService(
        AuthenticationStateProvider authenticationStateProvider,
        IJSRuntime jsRuntime,
        ILogger<BlazorAuthenticationService> logger)
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
