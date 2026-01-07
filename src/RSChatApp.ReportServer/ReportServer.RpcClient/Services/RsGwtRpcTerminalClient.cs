using System.Text;
using Microsoft.Extensions.Logging;
using ReportServer.Abstraction.Exceptions;
using ReportServer.RpcClient.DTOs;
using ReportServer.RpcClient.DTOs.Terminal;
using ReportServer.RpcClient.Infrastructure;

namespace ReportServer.RpcClient.Services;

public class RsGwtRpcTerminalClient : ReportServerGwtRpcClientBase
{
    private const string TerminalServiceHash = "BF140EBA9A84651D0CC50CCD75BC2C4F";
    private readonly ILogger _logger;

    public RsGwtRpcTerminalClient(ILogger logger, HttpClient httpClient, CookieContainerProvider cookieProvider) 
        : base(httpClient, cookieProvider)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes a new terminal session
    /// Based on traced request: initSession()
    /// </summary>
    public async Task<GwtRpcResponse<TerminalSessionInfoDto>> InitSessionAsync()
    {
        try
        {
            // Based on trace #2: 7|0|6|http://localhost:8090/reportserver/|C363EE187A6E3AED00BD381336F9868C|net.datenwerke.rs.terminal.client.terminal.rpc.TerminalRpcService|initSession|net.datenwerke.treedb.client.treedb.dto.AbstractNodeDto/45121059|net.datenwerke.gxtdto.client.dtomanager.Dto2PosoMapper|1|2|3|4|2|5|6|0|0|
            var payload = $"7|0|6|{_moduleBaseUrl}|{TerminalServiceHash}|net.datenwerke.rs.terminal.client.terminal.rpc.TerminalRpcService|initSession|net.datenwerke.treedb.client.treedb.dto.AbstractNodeDto/45121059|net.datenwerke.gxtdto.client.dtomanager.Dto2PosoMapper|1|2|3|4|2|5|6|0|0|";

            var response = await PostGwtRpcAsync("terminal", payload);

            if (string.IsNullOrEmpty(response))
                throw new ServerCallFailedException("Empty response from terminal init");
            
            _logger.LogDebug("InitSessionAsync response: {Response}", response);
            // Parse GWT response - trace shows: //OK[5,2,4,2,0,3,2,2,1,["java.util.HashMap/1797211028","java.lang.String/2004016611","pathWay","sessionId","58bf8974-255d-444c-b74e-02999d4983ba"],0,7]
            if (response.StartsWith("//OK"))
            {
                var stringTable = ExtractStringTable(response);
                if (stringTable.Count >= 5 && stringTable.Contains("sessionId"))
                {
                    var sessionIdIndex = stringTable.IndexOf("sessionId");
                    var sessionId = sessionIdIndex + 1 < stringTable.Count ? stringTable[sessionIdIndex + 1] : null;

                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        var sessionInfo = new TerminalSessionInfoDto
                        {
                            SessionId = sessionId,
                            Prompt = "groovy>", // Default prompt
                            WorkingDirectory = "/",
                            Environment = new Dictionary<string, string>()
                        };

                        return GwtRpcResponse<TerminalSessionInfoDto>.Successful(sessionInfo);
                    }
                }
                
            }
            
            if (TryParseException(response, out var responseException))
                return GwtRpcResponse<TerminalSessionInfoDto>.Fail(responseException);
            
            _logger.LogError("InitSessionAsync failed parsing response: {Response}", response);
            return GwtRpcResponse<TerminalSessionInfoDto>.Fail(new ServerCallFailedException("Failed to parse terminal session init response"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing terminal session: {Message}", ex.Message);
            return GwtRpcResponse<TerminalSessionInfoDto>.Fail(ex);
        }
    }

    /// <summary>
    /// Executes a command in the terminal session
    /// Based on traced request: execute(sessionId, command)
    /// </summary>
    public async Task<GwtRpcResponse<CommandResultDto>> ExecuteAsync(string sessionId, string command, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
                return GwtRpcResponse<CommandResultDto>.Fail(new InvalidDataException("Session ID is required"));
            
            if (string.IsNullOrEmpty(command))
                return GwtRpcResponse<CommandResultDto>.Fail(new InvalidDataException("Command is required"));

            // Based on trace #1: 7|0|7|http://localhost:8090/reportserver/|C363EE187A6E3AED00BD381336F9868C|net.datenwerke.rs.terminal.client.terminal.rpc.TerminalRpcService|execute|java.lang.String/2004016611|58bf8974-255d-444c-b74e-02999d4983ba|ls|1|2|3|4|2|5|5|6|7|
            var payload = $"7|0|7|{_moduleBaseUrl}|{TerminalServiceHash}|net.datenwerke.rs.terminal.client.terminal.rpc.TerminalRpcService|execute|java.lang.String/2004016611|{sessionId}|{command}|1|2|3|4|2|5|5|6|7|";

            var response = await PostGwtRpcAsync("terminal", payload, false, cancellationToken);

            if (string.IsNullOrEmpty(response))
               throw new ServerCallFailedException("No response received from server");
            
            _logger.LogDebug("Execute rs terminal command response: {Response}", response);
            // Parse GWT response - trace shows: //OK[0,0,0,0,0,0,0,0,-15,0,0,0,0,0,0,0,16,0,0,3,0,0,0,0,0,0,0,0,0,4,15,0,14,5,13,5,12,5,11,5,10,5,9,5,8,5,7,5,6,5,9,3,0,0,4,1,3,0,0,2,1,["net.datenwerke.rs.terminal.client.terminal.dto.decorator.CommandResultDtoDec/753283137","net.datenwerke.rs.terminal.client.terminal.dto.DisplayModeDto/1297612766","java.util.ArrayList/4159755760","net.datenwerke.rs.terminal.client.terminal.dto.decorator.CommandResultListDtoDec/3360806391","java.lang.String/2004016611","datasinks","datasources","reportmanager","dashboardlib","fileserver","remoteservers","transports","tsreport","usermanager","net.datenwerke.gxtdto.client.dtomanager.DtoView/2494148245","java.util.HashSet/3273092938"],0,7]
            if (response.StartsWith("//OK"))
            {
                var stringTable = ExtractStringTable(response);

                // Extract directory listing from string table
                var directoryList = stringTable.Where(s =>
                    !s.Contains("net.datenwerke") &&
                    !s.Contains("java.") &&
                    !string.IsNullOrWhiteSpace(s) &&
                    s.Length > 2 &&
                    !s.Contains("/")).ToList();

                var result = new CommandResultDto
                {
                    Result = string.Join("\n", directoryList),
                    Type = 1, // List type
                    Error = string.Empty,
                    Data = directoryList,
                    NewPrompt = "groovy>",
                    SessionClosed = false
                };

                return GwtRpcResponse<CommandResultDto>.Successful(result);
            }
            
            if (TryParseException(response, out var responseException))
                return GwtRpcResponse<CommandResultDto>.Fail(responseException);
            
            _logger.LogError("ExecuteAsync failed parsing response: {Response}", response);
            return GwtRpcResponse<CommandResultDto>.Fail(new ServerCallFailedException("Failed to parse terminal execute response"));
        }
        catch (Exception ex)
        {
            return GwtRpcResponse<CommandResultDto>.Fail(ex);
        }
    }

    /// <summary>
    /// Closes a terminal session
    /// Based on traced request: closeSession(sessionId)
    /// </summary>
    public async Task<GwtRpcResponse<bool>> CloseSessionAsync(string sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
                return GwtRpcResponse<bool>.Fail(new InvalidOperationException("Session ID cannot be null or empty"));

            var payload = $"7|0|7|{_moduleBaseUrl}|{TerminalServiceHash}|net.datenwerke.rs.terminal.client.terminal.rpc.TerminalRpcService|closeSession|java.lang.String|{sessionId}|1|2|3|4|1|5|6|7|";

            var response = await PostGwtRpcAsync("terminal", payload);
            if (string.IsNullOrEmpty(response))
                throw new ServerCallFailedException("Empty response from terminal close session");
            
            // Parse response - expect success indicator
            if (response.StartsWith("//OK"))
            {
                return GwtRpcResponse<bool>.Successful(true);
            }
            else if (response.Contains("true") || response.Contains("1"))
            {
                return GwtRpcResponse<bool>.Successful(true);
            }
            
            if (TryParseException(response, out var reponseException))
                return GwtRpcResponse<bool>.Fail(reponseException);
            
            _logger.LogError("CloseSessionAsync failed parsing response: {Response}", response);
            return GwtRpcResponse<bool>.Fail(new ServerCallFailedException("Failed to parse response for terminal close session"));
        }
        catch (Exception ex)
        {
            return GwtRpcResponse<bool>.Fail(ex);
        }
    }
    
}
