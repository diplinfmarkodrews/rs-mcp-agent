namespace ReportServer.RpcClient.Infrastructure;

/// <summary>
/// Carries the credentials of the current caller across the async flow of a request, so that outgoing
/// GWT-RPC calls are made on behalf of that caller instead of a process-wide shared session.
/// Legacy login: <see cref="CurrentJsessionId"/>. Keycloak: <see cref="CurrentBearerToken"/> (sent as Authorization
/// header) plus the JSESSIONID the ReportServer issued for that user, remembered under <see cref="CurrentBearerKey"/>
/// (see <see cref="CookieContainerProvider"/>).
/// Set by the authentication middleware of the MCP server; read by <see cref="Services.ReportServerGwtRpcClientBase"/>.
/// </summary>
public static class ReportServerSession
{
    public static readonly AsyncLocal<string?> CurrentJsessionId = new();
    public static readonly AsyncLocal<string?> CurrentBearerToken = new();

    /// <summary>Stable user key (Keycloak "sub"); access tokens rotate, the ReportServer session must not.</summary>
    public static readonly AsyncLocal<string?> CurrentBearerKey = new();
}
