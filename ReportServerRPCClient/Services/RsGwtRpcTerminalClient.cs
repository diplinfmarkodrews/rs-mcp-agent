using ReportServerRPCClient.DTOs.Terminal;
using ReportServerRPCClient.Infrastructure;
using System.Text;

namespace ReportServerRPCClient.Services;

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
    public async Task<TerminalSessionInfoDto?> InitSessionAsync()
    {
        var payload = new StringBuilder();
        payload.AppendLine($"7|0|6|{_moduleBaseUrl}|{TerminalServiceHash}|net.datenwerke.rs.terminal.service.terminal.TerminalService|initSession|java.lang.String|startTerminal|1|2|3|4|1|5|6|");

        var response = await PostGwtRpcAsync("net.datenwerke.rs.terminal.service.terminal.TerminalService", payload.ToString());

        if (response == null)
            return null;

        try
        {
            // Parse GWT response - expect format: //OK[sessionId, prompt, workingDirectory, environment]
            if (response.StartsWith("//OK["))
            {
                var jsonPart = response.Substring(5, response.Length - 6); // Remove //OK[ and ]
                var parts = ParseGwtStringArray(jsonPart);
                
                if (parts.Length >= 3)
                {
                    return new TerminalSessionInfoDto
                    {
                        SessionId = parts[0],
                        Prompt = parts[1],
                        WorkingDirectory = parts[2],
                        Environment = new Dictionary<string, string>()
                    };
                }
            }
            else
            {
                // Handle complex GWT serialized response
                var stringTable = ExtractStringTable(response);
                if (stringTable.Count >= 3)
                {
                    return new TerminalSessionInfoDto
                    {
                        SessionId = stringTable[0],
                        Prompt = stringTable[1], 
                        WorkingDirectory = stringTable[2],
                        Environment = new Dictionary<string, string>()
                    };
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse terminal session init response: {ex.Message}", ex);
        }

        return null;
    }

    /// <summary>
    /// Executes a command in the terminal session
    /// Based on traced request: exec(sessionId, command)
    /// </summary>
    public async Task<CommandResultDto?> ExecuteAsync(string sessionId, string command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(sessionId))
            throw new ArgumentNullException(nameof(sessionId));
        
        if (string.IsNullOrEmpty(command))
            throw new ArgumentNullException(nameof(command));

        var payload = new StringBuilder();
        payload.AppendLine($"7|0|8|{_moduleBaseUrl}|{TerminalServiceHash}|net.datenwerke.rs.terminal.service.terminal.TerminalService|exec|java.lang.String|java.lang.String|{sessionId}|{command}|1|2|3|4|2|5|6|7|8|");

        var response = await PostGwtRpcAsync("net.datenwerke.rs.terminal.service.terminal.TerminalService", payload.ToString(), cancellationToken);

        if (response == null)
            return null;

        try
        {
            // Parse GWT response for command result
            if (response.StartsWith("//OK["))
            {
                var jsonPart = response.Substring(5, response.Length - 6);
                var parts = ParseGwtStringArray(jsonPart);
                
                if (parts.Length >= 2)
                {
                    return new CommandResultDto
                    {
                        Result = parts[0],
                        Type = int.TryParse(parts[1], out var type) ? type : 0,
                        Error = parts.Length > 2 ? parts[2] : null,
                        NewPrompt = parts.Length > 3 ? parts[3] : null,
                        SessionClosed = parts.Length > 4 && bool.TryParse(parts[4], out var closed) && closed
                    };
                }
            }
            else
            {
                // Handle complex GWT serialized response
                var stringTable = ExtractStringTable(response);
                if (stringTable.Count >= 2)
                {
                    return new CommandResultDto
                    {
                        Result = stringTable[0],
                        Type = int.TryParse(stringTable[1], out var type) ? type : 0,
                        Error = stringTable.Count > 2 ? stringTable[2] : null,
                        NewPrompt = stringTable.Count > 3 ? stringTable[3] : null,
                        SessionClosed = stringTable.Count > 4 && bool.TryParse(stringTable[4], out var closed) && closed
                    };
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse terminal execute response: {ex.Message}", ex);
        }

        return null;
    }

    /// <summary>
    /// Closes a terminal session
    /// Based on traced request: closeSession(sessionId)
    /// </summary>
    public async Task<bool> CloseSessionAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            throw new ArgumentNullException(nameof(sessionId));

        var payload = new StringBuilder();
        payload.AppendLine($"7|0|7|http://localhost:8080/reportserver/|{TerminalServiceHash}|net.datenwerke.rs.terminal.service.terminal.TerminalService|closeSession|java.lang.String|{sessionId}|1|2|3|4|1|5|6|7|");

        var response = await PostGwtRpcAsync("net.datenwerke.rs.terminal.service.terminal.TerminalService", payload.ToString());

        if (response == null)
            return false;

        try
        {
            // Parse response - expect success indicator
            if (response.StartsWith("//OK"))
            {
                return true;
            }
            else if (response.Contains("true") || response.Contains("1"))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse terminal close response: {ex.Message}", ex);
        }

        return false;
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
