using RSChatApp.Web.Services.Terminal;

namespace RSChatApp.Web.Mcp.Tools;

public class TerminalPasteTool
{
    private readonly ITerminalManager _terminalManager;
    private readonly ILogger<TerminalPasteTool> _logger;

    public TerminalPasteTool(ILogger<TerminalPasteTool> logger, ITerminalManager terminalManager)
    {
        _logger = logger;
        _terminalManager = terminalManager;
    }

    public async Task<string> PasteCommand(string command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}