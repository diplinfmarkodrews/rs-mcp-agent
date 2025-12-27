using System.Text.Json;
using ReportServer.Abstraction.Contracts;
using ReportServer.Abstraction.Contracts.Terminal;
using RSChatApp.Infrastructure.ReportServer.Clients;
using RSChatApp.Web.Models.Terminal;

namespace RSChatApp.Web.Services.Terminal.Drivers;

public class RsTerminalDriver : ITerminalDriver
{
    private readonly ILogger<RsTerminalDriver> _logger;
    private readonly IRsTerminalClient _rsTerminalClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RsTerminalDriver(ILogger<RsTerminalDriver> logger,
        IHttpContextAccessor httpContextAccessor,
        IRsTerminalClient rsTerminalClient)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _rsTerminalClient = rsTerminalClient;
    }

    public Task<Result<TerminalSessionInfo>> InitSessionAsync(CancellationToken cancellationToken)
    {
        return _rsTerminalClient.InitTerminalSessionAsync(cancellationToken);
    }

    public async Task<Result<CommandResult>> ExecuteCommandAsync(string sessionId, string command, CancellationToken cancellationToken)
    {
       _logger.LogDebug("RsTerminalDriver.ExecuteCommandAsync: {Command}", command);
       var commandResult = await _rsTerminalClient.ExecuteCommandAsync(sessionId, command, cancellationToken);
       _logger.LogDebug("RsTerminalDriver.ExecuteCommandAsync: {CommandResult}", JsonSerializer.Serialize(commandResult));
       return commandResult;
    }

    public Task<Result> CloseSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        return _rsTerminalClient.CloseTerminalSessionAsync(sessionId, cancellationToken);
    }
    
    public Task<bool> ValidateSessionAsync(TerminalInstance terminal, SessionContext sessionContext, CancellationToken cancellationToken)
    {
        if (terminal.RsSessionId != sessionContext.RsSessionId)
            return Task.FromResult(false);
        return Task.FromResult(true);
    }

}

