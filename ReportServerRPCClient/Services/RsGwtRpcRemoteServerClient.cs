using AutoMapper;
using ReportServerRPCClient.DTOs.RemoteServer;
using ReportServerRPCClient.Infrastructure;

namespace ReportServerRPCClient.Services;

public class RsGwtRpcRemoteServerClient : ReportServerGwtRpcClientBase
{
    public RsGwtRpcRemoteServerClient(HttpClient httpClient, CookieContainerProvider cookieProvider) 
        : base(httpClient, cookieProvider)
    {
    }
    // Remote Server Manager Import - loadTree
    public async Task<List<ImportTreeModelDto>> LoadRemoteServerImportTreeAsync()
    {
        var payload = BuildGwtRpcPayload(
            "net.datenwerke.rs.remoteserver.client.remoteservermanager.eximport.im.rpc.RemoteServerManagerImportRpcService",
            "loadTree"
        );
        var response = await PostGwtRpcAsync("remoteservermanagerimport", payload);
        return ParseGwtResponse<List<ImportTreeModelDto>>(response);
    }

    

    // Remote Server Manager operations
    public async Task<string> LoadRemoteServerTreeAsync()
    {
        var payload = BuildGwtRpcPayload(
            "net.datenwerke.rs.remoteserver.client.remoteservermanager.rpc.RemoteServerManagerTreeHandlerRpcService",
            "loadTree"
        );

        return await PostGwtRpcAsync("remoteservermanager", payload);
    }

    public async Task<string> TestRemoteServerAsync(string serverDto)
    {
        var payload = BuildGwtRpcPayload(
            "net.datenwerke.rs.remotersrestserver.client.remotersrestserver.rpc.RemoteRsRestServerRpcService",
            "test",
            serverDto
        );

        return await PostGwtRpcAsync("remotersrestserver", payload);
    }

}

