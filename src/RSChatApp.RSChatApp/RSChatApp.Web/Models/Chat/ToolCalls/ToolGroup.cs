using RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient.Tools;
using RSChatApp.Shared.Infrastructure.Mcp.MetaData;

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

    public static List<ToolGroup> GroupFromProcessed(ProcessedMessage processed)
    {
        var groups = new List<ToolGroup>();
        ToolGroup? current = null;

        foreach (var invocation in processed.Invocations)
        {
            if (current is null || current.Type != invocation.Type)
            {
                current = new ToolGroup(invocation.Type);
                groups.Add(current);
            }

            current.Invocations.Add(invocation);
            current.Results.Add(processed.Results.GetValueOrDefault(invocation.CallId));
        }

        return groups;
    }
}
