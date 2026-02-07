namespace RSChatApp.Web.Models.Chat.ToolCalls;

/// <summary>
/// Presentation hints for rendering tool calls/results in the chat UI.
/// These are intentionally independent from the renderer implementation.
/// </summary>
public record ToolUiHints(
    bool DefaultExpanded,
    bool Collapsible = true
)
{
    public static ToolUiHints Default => new(
        DefaultExpanded: false,
        Collapsible: true);
}
