using Microsoft.Extensions.AI;
using RSChatApp.Web.Models.Chat.ToolCalls;
using RSChatApp.Web.Services.Chat.Tools;
using System.Text.Json;

namespace RSChatApp.Web.Services.Chat;

public class ToolCallProcessor
{
    private readonly ToolRegistry _registry;
    private readonly ILogger<ToolCallProcessor> _logger;
    private readonly ToolInvocationFactory _toolInvocatiobFactory;
    private readonly ToolResultFactory _toolResultFactory;

    public ToolCallProcessor(ToolRegistry registry, 
        ToolInvocationFactory toolInvocationFactory, 
        ToolResultFactory toolResultFactory, 
        ILogger<ToolCallProcessor> logger)
    {
        _registry = registry;
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

    private ToolInvocation CreateInvocation(FunctionCallContent fcc)
    {
        var descriptor = _registry.GetDescriptor(fcc.Name);
        var parameters = fcc.Arguments?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                      ?? new Dictionary<string, object?>();
        var result = new ToolInvocation(
            CallId: fcc.CallId,
            Type: descriptor.Type,
            RawName: fcc.Name,
            DisplayName: descriptor.GetDisplayName(parameters),
            Parameters: parameters,
            Metadata: descriptor.ExtractMetadata(parameters),
            Permissions: descriptor.GetPermissions(parameters),
            UiHints: descriptor.GetUiHints(parameters)
        );
        return result;
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
