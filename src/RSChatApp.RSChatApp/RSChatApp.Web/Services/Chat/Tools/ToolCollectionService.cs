using Microsoft.Extensions.AI;

namespace RSChatApp.Web.Services.Chat.Tools;

public class ToolCollectionService
{
    private readonly List<AITool> _aiTools;
    public ToolCollectionService(IEnumerable<AITool> aiTools)
    {
        _aiTools = aiTools.ToList();
    }

    public ToolCollectionService()
    {
        _aiTools = new List<AITool>();
    }
    public List<AITool> AllTools => _aiTools;    
}
