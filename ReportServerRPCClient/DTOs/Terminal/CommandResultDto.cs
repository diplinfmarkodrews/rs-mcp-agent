using Newtonsoft.Json;

namespace ReportServerRPCClient.DTOs.Terminal;

public class CommandResultDto
{
    [JsonProperty("result")]
    public string Result { get; set; }

    [JsonProperty("type")]
    public int Type { get; set; }

    [JsonProperty("error")]
    public string Error { get; set; }

    [JsonProperty("data")]
    public object Data { get; set; }

    [JsonProperty("newPrompt")]
    public string NewPrompt { get; set; }

    [JsonProperty("sessionClosed")]
    public bool SessionClosed { get; set; }
}

