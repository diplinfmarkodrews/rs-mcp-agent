using System.Text.Json;
using ReportServer.Abstraction.Contracts;
using ReportServer.Abstraction.Contracts.Terminal;
using RSChatApp.Infrastructure.ReportServer.Clients;

namespace RSChatApp.Web.Services.Terminal.Drivers;

public class RsTerminalDriver : ITerminalDriver
{
    private readonly ILogger<RsTerminalDriver> _logger;
    private readonly IRsTerminalClient _rsTerminalClient;

    public RsTerminalDriver(ILogger<RsTerminalDriver> logger, IRsTerminalClient rsTerminalClient)
    {
        _logger = logger;
        _rsTerminalClient = rsTerminalClient;
    }

    public Task<Result<TerminalSessionInfo>> InitSessionAsync(CancellationToken cancellationToken)
    {
        return _rsTerminalClient.InitTerminalSessionAsync(cancellationToken);
    }

    public async Task<Result<CommandResult>> ExecuteCommandAsync(string sessionId, string command, CancellationToken cancellationToken)
    {
       var commandResult = await _rsTerminalClient.ExecuteCommandAsync(sessionId, command, cancellationToken);
       _logger.LogDebug("RsTerminalDriver.ExecuteCommandAsync: {CommandResult}", JsonSerializer.Serialize(commandResult));
       return commandResult;
    }

    public Task<Result> CloseSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        return _rsTerminalClient.CloseTerminalSessionAsync(sessionId, cancellationToken);
    }

    public Task<bool> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        // TODO: Validation with authentication client
        return Task.FromResult(true);
    }
}
