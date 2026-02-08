using RSChatApp.Web.Models.Chat.ToolCalls;

namespace RSChatApp.Web.Services.Chat.Tools.Descriptors;

public class TerminalToolDescriptor : IToolDescriptor
{
    public ToolType Type => ToolType.TerminalExecute;

    public string GetDisplayName(IReadOnlyDictionary<string, object?> parameters)
    {
        var cmd = FormatValue(parameters.GetValueOrDefault("command")) 
               ?? FormatValue(parameters.GetValueOrDefault("cmd"));
        
        return $"Terminal: {Truncate(cmd, 50)}";
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
        var sessionId = FormatValue(parameters.GetValueOrDefault("sessionId"));
        var command = FormatValue(parameters.GetValueOrDefault("command"))
                   ?? FormatValue(parameters.GetValueOrDefault("cmd"));
        
        return new ToolMetadata(
            SessionId: sessionId,
            Timestamp: DateTime.UtcNow,
            TargetInfo: command
        );
    }

    public ToolUiHints GetUiHints(IReadOnlyDictionary<string, object?> parameters)
    {
        // Terminal output is usually the primary thing users want to read.
        return new ToolUiHints(DefaultExpanded: true);
    }

    public string GetIconSvg()
    {
        return """
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M6.75 7.5l3 2.25-3 2.25m4.5 0h3m-9 8.25h13.5A2.25 2.25 0 0021 18V6a2.25 2.25 0 00-2.25-2.25H5.25A2.25 2.25 0 003 6v12a2.25 2.25 0 002.25 2.25z" />
            </svg>
        """;
    }

    public string GetColorClass() => "tool-terminal";

    private static string? FormatValue(object? value)
    {
        return value switch
        {
            null => null,
            string s => s,
            System.Text.Json.JsonElement e when e.ValueKind == System.Text.Json.JsonValueKind.String => e.GetString(),
            _ => value.ToString()
        };
    }

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= max ? value : value[..max] + "…";
    }
}
