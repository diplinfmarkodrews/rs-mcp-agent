namespace RSChatApp.Web.Models.Terminal;

public class TerminalInstance
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required TerminalType Type { get; init; }
    public string? SessionId { get; set; }
    public required string Name { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public List<CommandEntry> CommandHistory { get; init; } = new();
    public bool IsMinimized { get; set; }
    public bool IsValid { get; set; } = true;
    public string? WorkingDirectory { get; set; }
    public string? Prompt { get; set; }
}

public class CommandEntry
{
    public required string Command { get; init; }
    public required string Output { get; init; }
    public bool IsSuccess { get; init; }
    public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;
    public string? Error { get; init; }
}

public enum TerminalType
{
    ReportServer,
    JavaScript
}