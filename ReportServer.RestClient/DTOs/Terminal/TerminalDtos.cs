using ReportServer.Abstraction.Contracts;

namespace ReportServer.RestClient.DTOs.Terminal;

public class InitSessionRequestDto
{
    public AbstractNodeDto? Node { get; set; }
    public Dictionary<string, string>? Mapper { get; set; }
}

public class AbstractNodeDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class TerminalSessionInfoDto
{
    public string? SessionId { get; set; }
    public string? Prompt { get; set; }
    public string? WorkingDirectory { get; set; }
}

public class ExecuteCommandRequestDto
{
    public string Command { get; set; } = string.Empty;
}

public class CommandResultDto
{
    public string? Result { get; set; }
    public int ExitCode { get; set; }
    public string? Error { get; set; }
    public string? Data { get; set; }
    public string? NewPrompt { get; set; }
}
