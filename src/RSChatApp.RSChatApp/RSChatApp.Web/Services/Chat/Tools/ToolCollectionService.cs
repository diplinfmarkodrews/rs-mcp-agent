using Microsoft.Extensions.AI;

namespace RSChatApp.Web.Services.Chat.Tools;

public class ToolCollectionService
{
    private readonly Dictionary<string, List<AITool>> _grouped;

    public ToolCollectionService(Dictionary<string, List<AITool>> grouped)
    {
        _grouped = grouped;
    }

    public IReadOnlyDictionary<string, List<AITool>> GroupedTools => _grouped;
    public List<AITool> AllTools => _grouped.Values.SelectMany(t => t).ToList();
}
