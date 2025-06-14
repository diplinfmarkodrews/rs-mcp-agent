using ReportServerPort.Authentication.Contracts;
using ReportServerPort.Contracts;
using ReportServerPort.Contracts.Terminal;
using ReportServerPort.FileServer.Contracts;

namespace ReportServerPort;

public interface IReportServerClient : 
    IRsAuthenticationClient,
    // IRsFileServerClient,
    IRsTerminalClient,
    IDisposable
{
    
//     Task<string> LoadRemoteServerImportTreeAsync();
//     Task<string> LoadFileTreeAsync();
//     Task<string> LoadFileDataAsStringAsync(long fileId);
//     Task<string> UpdateFileAsync(string fileDto, string content);
//     Task<string> LoadRemoteServerTreeAsync();
//     Task<string> TestRemoteServerAsync(string serverDto);
}

public interface IRsAuthenticationClient
{
    Task<Result<AuthenticationResult>> AuthenticateAsync(string username, string password);
}

public interface IRsFileServerClient
{
    Task<Result<List<FileTreeNode>>> LoadFileTreeAsync();
    Task<Result<string>> LoadFileDataAsStringAsync(long fileId);
    // Task<Result<string>> UpdateFileAsync(FileDto fileDto, string content);
}
public interface IRsTerminalClient
{
    Task<Result> CloseSessionAsync(string sessionId);

    Task<Result<TerminalSessionInfo>> InitSessionAsync(AbstractNode node = null,
        Dictionary<string, string> mapper = null);
    Task<Result<CommandResult>> ExecuteAsync(string sessionId, string command, CancellationToken cancellationToken = default);
    Task<Result<CommandResult>> CtrlCPressedAsync(string sessionId);
    //ToDo: Add AutoComplete 
}
