using ReportServerPort.Contracts;

namespace ReportServerPort.Authentication.Contracts;

public class AuthenticationResult : IContract
{
    public bool IsAuthenticated { get; set; }
    public User User { get; set; }
    public string SessionId { get; set; }
}

public class User
{
    public long Id { get; set; }
    public string Username { get; set; }
    public string Firstname { get; set; }
    public string Lastname { get; set; }
    public string Email { get; set; }
    public bool Active { get; set; }
    public bool SuperUser { get; set; }
    public Dictionary<string, string> Properties { get; set; }
    public List<Group> Groups { get; set; }
}

public class Group
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}

public class LoginRestrictions
{
    public int MaxLoginAttempts { get; set; }
    public int AccountLockoutDuration { get; set; }
    public int PasswordExpirationDays { get; set; }
}