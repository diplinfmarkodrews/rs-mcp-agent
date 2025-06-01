using AutoMapper;
using ReportServerPort.Contracts;
using ReportServerRPCClient.Infrastructure;

namespace ReportServerRPCClient.Services;

public class RsGwtRpcFileServerClient : ReportServerGwtRpcClientBase
{
    public RsGwtRpcFileServerClient(HttpClient httpClient, CookieContainerProvider cookieProvider) 
        : base(httpClient, cookieProvider)
    {
    }
    
    // File Server operations
    public async Task<string> LoadFileTreeAsync()
    {
        var payload = BuildGwtRpcPayload(
            "net.datenwerke.rs.fileserver.client.fileserver.rpc.FileServerRpcService",
            "loadTree"
        );

        return await PostGwtRpcAsync("fileserver", payload);
    }

    public async Task<string> LoadFileDataAsStringAsync(long fileId)
    {
        var payload = BuildGwtRpcPayload(
            "net.datenwerke.rs.fileserver.client.fileserver.rpc.FileServerRpcService",
            "loadFileDataAsString",
            fileId
        );

        return await PostGwtRpcAsync("fileserver", payload);
    }

    public async Task<string> UpdateFileAsync(string fileDto, string content)
    {
        var payload = BuildGwtRpcPayload(
            "net.datenwerke.rs.fileserver.client.fileserver.rpc.FileServerRpcService",
            "updateFile",
            fileDto, content
        );

        return await PostGwtRpcAsync("fileserver", payload);
    }
}

