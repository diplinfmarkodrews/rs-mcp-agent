using Newtonsoft.Json;

namespace ReportServer.RestClient.DTOs.Terminal;

public class TerminalSessionRequest
{
    [JsonProperty("nodeId")]
    public long? NodeId { get; set; }
}

public class TerminalSessionResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("sessionId")]
    public string? SessionId { get; set; }

    [JsonProperty("pathWay")]
    public string? PathWay { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }
}

public class TerminalExecuteRequest
{
    [JsonProperty("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonProperty("command")]
    public string Command { get; set; } = string.Empty;
}

public class TerminalExecuteResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("result")]
    public CommandResultDto? Result { get; set; }
}