using Microsoft.Extensions.AI;
using RSChatApp.Web.Models.Chat.ToolCalls;

namespace RSChatApp.Web.Services.Chat.Tools;

public class ToolCallProcessor
{

    private readonly ILogger<ToolCallProcessor> _logger;
    private readonly ToolInvocationFactory _toolInvocatiobFactory;
    private readonly ToolResultFactory _toolResultFactory;

    public ToolCallProcessor(ToolInvocationFactory toolInvocationFactory, 
        ToolResultFactory toolResultFactory, 
        ILogger<ToolCallProcessor> logger)
    {
        _toolInvocatiobFactory = toolInvocationFactory;
        _toolResultFactory = toolResultFactory;
        _logger = logger;
    }
    
    public ProcessedMessage ProcessMessage(ChatMessage message)
    {
        var textContents = new List<string>();
        var invocations = new List<ToolInvocation>();
        var results = new Dictionary<string, ToolResult>();
        
        foreach (var content in message.Contents)
        {
            
            if (content is TextContent tc && !string.IsNullOrWhiteSpace(tc.Text))
            {
                textContents.Add(tc.Text);
            }
            else if (content is FunctionCallContent fcc)
            {
                invocations.Add(_toolInvocatiobFactory.Create(fcc));
            }
            else if (content is FunctionResultContent frc)
            {
                results[frc.CallId] = _toolResultFactory.Create(frc, invocations);
            }
        }

        var groups = GroupConsecutiveTools(invocations, results);

        return new ProcessedMessage(
            OriginalMessage: message,
            TextContent: string.Join("\n", textContents),
            ToolGroups: groups
        );
    }
    
    private List<ToolGroup> GroupConsecutiveTools(
        List<ToolInvocation> invocations,
        Dictionary<string, ToolResult> results)
    {
        var groups = new List<ToolGroup>();
        ToolGroup? currentGroup = null;

        foreach (var invocation in invocations)
        {
            if (currentGroup is null || currentGroup.Type != invocation.Type)
            {
                currentGroup = new ToolGroup(invocation.Type);
                groups.Add(currentGroup);
            }

            currentGroup.Invocations.Add(invocation);
            currentGroup.Results.Add(results.GetValueOrDefault(invocation.CallId));
        }

        return groups;
    }
    
}
