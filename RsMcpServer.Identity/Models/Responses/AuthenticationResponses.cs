namespace RsMcpServer.Identity.Models.Responses;

/// <summary>
/// Response model for login operations
/// </summary>
public record LoginResponse(
    bool Success, 
    string? Token, 
    UserInfo? User, 
    DateTime? ExpiresAt, 
    string? Error);

/// <summary>
/// Response model for token refresh operations
/// </summary>
public record RefreshResponse(
    bool Success, 
    string? Token, 
    DateTime? ExpiresAt, 
    string? Error);

/// <summary>
/// Response model for logout operations
/// </summary>
public record LogoutResponse(bool Success, string? Error);

/// <summary>
/// User information model for API responses
/// </summary>
public record UserInfo(
    string Id,
    string Username,
    string? Email,
    string? FirstName,
    string? LastName,
    string[] Roles,
    Dictionary<string, object> Properties);
