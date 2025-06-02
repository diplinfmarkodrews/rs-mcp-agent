namespace ReportServerPort.Contracts.Terminal;

public class CommandResult
{
    public string Result { get; set; }
    public CommandResultType Type { get; set; }
    public string Error { get; set; }
    public object Data { get; set; }
    public string NewPrompt { get; set; }
    public bool SessionClosed { get; set; }
}

public enum CommandResultType
{
    Success,
    Error,
    Info,
    Warning,
    Prompt
}