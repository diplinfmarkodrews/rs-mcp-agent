using ReportServer.Abstraction.Contracts.Terminal;
using RSChatApp.Common;
using RSChatApp.Web.Models.Terminal;

namespace RSChatApp.Web.Services.Terminal.Drivers;

// interface to handle terminal sessions for different providers
public interface ITerminalDriver
{
    Task<Result<TerminalSessionInfo>> InitSessionAsync(CancellationToken cancellationToken);

    Task<Result<CommandResult>> ExecuteCommandAsync(string sessionId, string command,
        CancellationToken cancellationToken);

    Task<Result> CloseSessionAsync(string sessionId, CancellationToken cancellationToken);

    Task<bool> ValidateSessionAsync(TerminalInstance terminal, SessionContext sessionContext, CancellationToken cancellationToken);
}

public record SessionContext(string RsSessionId);
