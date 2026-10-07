using Microsoft.Extensions.AI;

namespace RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient.Tools;

public interface IToolCollectionService
{
    IReadOnlyList<AITool> AllTools { get; }
}
public class ToolCollectionService : IToolCollectionService
{
    private readonly Dictionary<string, List<AITool>> _grouped;

    public ToolCollectionService(Dictionary<string, List<AITool>> grouped)
    {
        _grouped = grouped;
    }

    public void AddTools(List<AITool> tools)
    {
        
    }
    public IReadOnlyDictionary<string, List<AITool>> GroupedTools => _grouped;
    public IReadOnlyList<AITool> AllTools => _grouped.Values.SelectMany(t => t).ToList();
}
