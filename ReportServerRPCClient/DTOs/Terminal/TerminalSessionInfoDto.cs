using Newtonsoft.Json;

namespace ReportServerRPCClient.DTOs.Terminal;

public class TerminalSessionInfoDto
{
    [JsonProperty("sessionId")]
    public string SessionId { get; set; }

    [JsonProperty("prompt")]
    public string Prompt { get; set; }

    [JsonProperty("workingDirectory")]
    public string WorkingDirectory { get; set; }

    [JsonProperty("environment")]
    public Dictionary<string, string> Environment { get; set; } = new Dictionary<string, string>();
}