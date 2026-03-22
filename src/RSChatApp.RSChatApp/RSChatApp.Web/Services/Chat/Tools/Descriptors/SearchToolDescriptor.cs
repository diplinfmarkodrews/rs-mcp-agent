using RSChatApp.Shared.Infrastructure.Mcp.MetaData;
using RSChatApp.Web.Models.Chat.ToolCalls;

namespace RSChatApp.Web.Services.Chat.Tools.Descriptors;

public class SearchToolDescriptor : IToolDescriptor
{
    public ToolType Type => ToolType.Search;

    public ResultContentType ResultContentType => ResultContentType.SearchCitations;
    public string GetDisplayName(IReadOnlyDictionary<string, object?> parameters)
    {
        var phrase = FormatValue(parameters.GetValueOrDefault("searchPhrase"));
        var filename = FormatValue(parameters.GetValueOrDefault("filenameFilter"));
        
        if (!string.IsNullOrWhiteSpace(filename))
        {
            return $"Search: {phrase} in {filename}";
        }
        
        return $"Search: {phrase}";
    }

    public ToolPermissions GetPermissions(IReadOnlyDictionary<string, object?> parameters)
    {
        return new ToolPermissions(
            CanRerun: true,
            CanEditResult: false,
            CanCopy: true,
            CanExpand: true
        );
    }

    public ToolMetadata ExtractMetadata(IReadOnlyDictionary<string, object?> parameters)
    {
        var phrase = FormatValue(parameters.GetValueOrDefault("searchPhrase"));
        
        return new ToolMetadata(
            SessionId: null,
            Timestamp: DateTime.UtcNow,
            TargetInfo: phrase
        );
    }

    public ToolUiHints GetUiHints(IReadOnlyDictionary<string, object?> parameters)
    {
        // Search results can be long/noisy; keep collapsed unless the user expands.
        return ToolUiHints.Default;
    }

    public ToolUserConfirmation GetUserConfirmation(string? functionName = null)
    {
        return ToolUserConfirmation.None;
    }

    public string GetIconSvg()
    {
        return """
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z" />
            </svg>
        """;
    }

    public IEnumerable<string> ToolNames
    {
        get => ["Search", "search"];
    }

    public string GetColorClass() => "tool-search";

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string s => s,
            System.Text.Json.JsonElement e when e.ValueKind == System.Text.Json.JsonValueKind.String => e.GetString() ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }
}
