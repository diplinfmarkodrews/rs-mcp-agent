namespace RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient.Tools;
public record ToolUiHints(
    bool DefaultExpanded,
    bool Collapsible = true
)
{
    public static ToolUiHints Default => new(
        DefaultExpanded: false,
        Collapsible: true);
}
