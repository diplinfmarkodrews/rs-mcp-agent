using ReportServerPort.Contracts;

namespace ReportServerPort;

public interface IReportServerClient : 
    IRsAuthenticationClient,
    // IRsFileServerClient,
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
    Task<Result<List<FileTreeNodeResult>>> LoadFileTreeAsync();
    Task<Result<string>> LoadFileDataAsStringAsync(long fileId);
    // Task<Result<string>> UpdateFileAsync(FileDto fileDto, string content);
}
