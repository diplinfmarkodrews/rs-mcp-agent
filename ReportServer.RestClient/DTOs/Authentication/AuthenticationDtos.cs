namespace ReportServer.RestClient.DTOs.Authentication;

public class AuthenticationRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthenticationResponseDto
{
    public bool Success { get; set; }
    public UserDto? User { get; set; }
    public string? SessionId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UserDto
{
    public long Id { get; set; }
    public string? Username { get; set; }
    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public string? Email { get; set; }
    public Dictionary<string, string>? Properties { get; set; }
    public List<GroupDto>? Groups { get; set; }
}

public class GroupDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
