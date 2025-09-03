using System.Text;
using ReportServer.RpcClient.DTOs;
using ReportServer.RpcClient.DTOs.Terminal;
using ReportServer.RpcClient.Infrastructure;

namespace ReportServer.RpcClient.Services;

public class RsGwtRpcTerminalClient : ReportServerGwtRpcClientBase
{
    private const string TerminalServiceHash = "C363EE187A6E3AED00BD381336F9868C";

    public RsGwtRpcTerminalClient(HttpClient httpClient, CookieContainerProvider cookieProvider) 
        : base(httpClient, cookieProvider)
    {
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
            {
                return GwtRpcResponse<TerminalSessionInfoDto>.Fail("Empty response from terminal init");
            }

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

                        return GwtRpcResponse<TerminalSessionInfoDto>.Successful("Terminal session initialized successfully", sessionInfo);
                    }
                }
            }

            return GwtRpcResponse<TerminalSessionInfoDto>.Fail("Failed to parse terminal session init response");
        }
        catch (Exception ex)
        {
            return GwtRpcResponse<TerminalSessionInfoDto>.Fail($"Failed to initialize terminal session: {ex.Message}", ex);
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
                return GwtRpcResponse<CommandResultDto>.Fail("Session ID cannot be null or empty");
            
            if (string.IsNullOrEmpty(command))
                return GwtRpcResponse<CommandResultDto>.Fail("Command cannot be null or empty");

            // Based on trace #1: 7|0|7|http://localhost:8090/reportserver/|C363EE187A6E3AED00BD381336F9868C|net.datenwerke.rs.terminal.client.terminal.rpc.TerminalRpcService|execute|java.lang.String/2004016611|58bf8974-255d-444c-b74e-02999d4983ba|ls|1|2|3|4|2|5|5|6|7|
            var payload = $"7|0|7|{_moduleBaseUrl}|{TerminalServiceHash}|net.datenwerke.rs.terminal.client.terminal.rpc.TerminalRpcService|execute|java.lang.String/2004016611|{sessionId}|{command}|1|2|3|4|2|5|5|6|7|";

            var response = await PostGwtRpcAsync("terminal", payload, cancellationToken);

            if (string.IsNullOrEmpty(response))
            {
                return GwtRpcResponse<CommandResultDto>.Fail("Empty response from terminal execute");
            }

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

                return GwtRpcResponse<CommandResultDto>.Successful("Command executed successfully", result);
            }

            return GwtRpcResponse<CommandResultDto>.Fail("Failed to parse terminal execute response");
        }
        catch (Exception ex)
        {
            return GwtRpcResponse<CommandResultDto>.Fail($"Failed to execute terminal command: {ex.Message}", ex);
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
                return GwtRpcResponse<bool>.Fail("Session ID cannot be null or empty");

            var payload = $"7|0|7|{_moduleBaseUrl}|{TerminalServiceHash}|net.datenwerke.rs.terminal.client.terminal.rpc.TerminalRpcService|closeSession|java.lang.String|{sessionId}|1|2|3|4|1|5|6|7|";

            var response = await PostGwtRpcAsync("terminal", payload);

            if (string.IsNullOrEmpty(response))
            {
                return GwtRpcResponse<bool>.Fail("Empty response from terminal close session");
            }

            // Parse response - expect success indicator
            if (response.StartsWith("//OK"))
            {
                return GwtRpcResponse<bool>.Successful("Terminal session closed successfully", true);
            }
            else if (response.Contains("true") || response.Contains("1"))
            {
                return GwtRpcResponse<bool>.Successful("Terminal session closed successfully", true);
            }

            return GwtRpcResponse<bool>.Fail("Failed to close terminal session");
        }
        catch (Exception ex)
        {
            return GwtRpcResponse<bool>.Fail($"Failed to close terminal session: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parses GWT string array format
    /// </summary>
    private string[] ParseGwtStringArray(string gwtArray)
    {
        if (string.IsNullOrEmpty(gwtArray))
            return Array.Empty<string>();

        // Remove quotes and split by comma
        var cleaned = gwtArray.Trim('"');
        return cleaned.Split(',').Select(s => s.Trim('"')).ToArray();
    }

    /// <summary>
    /// Extracts string table from complex GWT serialized response
    /// </summary>
    private List<string> ExtractStringTable(string gwtResponse)
    {
        var stringTable = new List<string>();
        
        if (string.IsNullOrEmpty(gwtResponse))
            return stringTable;

        try
        {
            // Look for string literals in GWT response
            var lines = gwtResponse.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("\"") && !line.StartsWith("//"))
                {
                    var matches = System.Text.RegularExpressions.Regex.Matches(line, "\"([^\"]+)\"");
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        if (match.Groups.Count > 1)
                        {
                            stringTable.Add(match.Groups[1].Value);
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // Fallback: try to extract any quoted strings
            var matches = System.Text.RegularExpressions.Regex.Matches(gwtResponse, "\"([^\"]+)\"");
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    stringTable.Add(match.Groups[1].Value);
                }
            }
        }

        return stringTable;
    }
}
