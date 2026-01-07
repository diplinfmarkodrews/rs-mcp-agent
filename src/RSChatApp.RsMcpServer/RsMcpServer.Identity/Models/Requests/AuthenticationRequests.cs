namespace RsMcpServer.Identity.Models.Requests;

/// <summary>
/// Request model for user login
/// </summary>
public record LoginRequest(string Username, string Password);

/// <summary>
/// Request model for token refresh
/// </summary>
public record RefreshRequest(string Token);

/// <summary>
/// Request model for user logout
/// </summary>
public record LogoutRequest(string Token);
