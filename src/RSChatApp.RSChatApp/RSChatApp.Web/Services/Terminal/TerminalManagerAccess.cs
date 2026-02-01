using RSChatApp.Web.Models.Terminal;

namespace RSChatApp.Web.Services.Terminal;

public class TerminalManagerAccess
{
    private readonly ITerminalManager _terminalManager;

    public TerminalManagerAccess(ITerminalManager terminalManager)
    {
        _terminalManager = terminalManager;
    }

    public async Task<Guid> GetActiveTerminalIdAsync(TerminalType terminalType, CancellationToken cancellationToken = default)
    {
        var activeTerminal = _terminalManager.ActiveTerminal;
        if (activeTerminal != null && activeTerminal.Type == terminalType && activeTerminal.IsValid)
            return activeTerminal.Id;
        
        var validTerminal = _terminalManager.Terminals.FirstOrDefault(t => t.Type == terminalType && t.IsValid);
        if (validTerminal != null)
            return validTerminal.Id;
        
        var newTerminal = await _terminalManager.CreateAsync(terminalType, cancellationToken);
        await _terminalManager.SetActiveAsync(newTerminal.Id, cancellationToken);
        return newTerminal.Id;
    }
}