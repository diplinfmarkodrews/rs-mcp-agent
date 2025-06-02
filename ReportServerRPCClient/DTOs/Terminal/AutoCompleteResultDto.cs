using Newtonsoft.Json;

namespace ReportServerRPCClient.DTOs.Terminal;

public class AutocompleteResultDto
{
    [JsonProperty("suggestions")]
    public List<string> Suggestions { get; set; } = new List<string>();

    [JsonProperty("completedCommand")]
    public string CompletedCommand { get; set; }

    [JsonProperty("replacementStart")]
    public int ReplacementStart { get; set; }

    [JsonProperty("replacementEnd")]
    public int ReplacementEnd { get; set; }

    [JsonProperty("hasMoreSuggestions")]
    public bool HasMoreSuggestions { get; set; }
}