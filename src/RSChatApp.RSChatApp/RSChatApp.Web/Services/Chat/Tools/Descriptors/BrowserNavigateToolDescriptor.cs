using RSChatApp.Shared.Infrastructure.Mcp.MetaData;
using RSChatApp.Web.Models.Chat.ToolCalls;

namespace RSChatApp.Web.Services.Chat.Tools.Descriptors;

public class BrowserNavigateToolDescriptor : IToolDescriptor
{
    public ToolType Type { get => ToolType.BrowserNavigate; }
     public string GetDisplayName(IReadOnlyDictionary<string, object?> parameters)
    {
        var functionName = FormatValue(parameters.GetValueOrDefault("functionName"));
        
        return $"Browser: {Truncate(functionName, 50)}";
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
        var script = FormatValue(parameters.GetValueOrDefault("script"))
                  ?? FormatValue(parameters.GetValueOrDefault("code"));
        
        return new ToolMetadata(
            SessionId: sessionId,
            Timestamp: DateTime.UtcNow,
            TargetInfo: script
        );
    }

    public ToolUiHints GetUiHints(IReadOnlyDictionary<string, object?> parameters)
    {
        // Browser execute JavaScript results are typically immediately relevant.
        return new ToolUiHints(DefaultExpanded: true);
    }

    public ToolUserConfirmation GetUserConfirmation(string? functionName = null)
    {
        return ToolUserConfirmation.ToolResultOnly;
    }

    public string GetIconSvg()
    {
        return """
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 21a9.004 9.004 0 008.716-6.747M12 21a9.004 9.004 0 01-8.716-6.747M12 21c2.485 0 4.5-4.03 4.5-9S14.485 3 12 3m0 18c-2.485 0-4.5-4.03-4.5-9S9.515 3 12 3m0 0a8.997 8.997 0 017.843 4.582M12 3a8.997 8.997 0 00-7.843 4.582m15.686 0A11.953 11.953 0 0112 10.5c-2.998 0-5.74-1.1-7.843-2.918m15.686 0A8.959 8.959 0 0121 12c0 .778-.099 1.533-.284 2.253m0 0A17.919 17.919 0 0112 16.5c-3.162 0-6.133-.815-8.716-2.247m0 0A9.015 9.015 0 013 12c0-1.605.42-3.113 1.157-4.418" />
            </svg>
        """;
    }

    public string GetColorClass() => "tool-browser";

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