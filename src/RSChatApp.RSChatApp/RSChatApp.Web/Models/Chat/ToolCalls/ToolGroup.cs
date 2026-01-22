namespace RSChatApp.Web.Models.Chat.ToolCalls;

public class ToolGroup
{
    public ToolType Type { get; set; }
    public List<ToolInvocation> Invocations { get; set; } = new();
    public List<ToolResult?> Results { get; set; } = new();
    public bool IsCollapsed { get; set; }

    public ToolGroup(ToolType type)
    {
        Type = type;
    }
}
