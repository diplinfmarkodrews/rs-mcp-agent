namespace ReportServer.RpcClient.Infrastructure;

/// <summary>
/// Carries the ReportServer session id (JSESSIONID) of the current caller across the async flow of a request,
/// so that outgoing GWT-RPC calls are made on behalf of that caller instead of a process-wide shared session.
/// Set by the authentication middleware of the MCP server; read by <see cref="Services.ReportServerGwtRpcClientBase"/>.
/// </summary>
public static class ReportServerSession
{
    public static readonly AsyncLocal<string?> CurrentJsessionId = new();
}
