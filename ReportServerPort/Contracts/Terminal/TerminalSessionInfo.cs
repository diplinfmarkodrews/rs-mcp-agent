namespace ReportServerPort.Contracts.Terminal;

public class TerminalSessionInfo
{
    public string SessionId { get; set; }
    public string Prompt { get; set; }
    public string WorkingDirectory { get; set; }
    public Dictionary<string, string> Environment { get; set; } = new Dictionary<string, string>();
}