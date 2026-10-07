using Microsoft.Extensions.Logging;
using RSChatApp.Application.Core.Chat.Dtos;

namespace RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient.Tools;

public class ToolCallProcessor(
    ToolInvocationFactory toolInvocationFactory,
    ToolResultFactory toolResultFactory,
    ILogger<ToolCallProcessor> logger)
{
    public ProcessedMessage ProcessMessage(ChatMessageDto message)
    {
        var invocations = new List<ToolInvocation>();
        var results = new Dictionary<string, ToolResult>();

        if (message.ToolCalls is not null)
        {
            foreach (var doc in message.ToolCalls)
            {
                var invocation = toolInvocationFactory.Create(doc);
                invocations.Add(invocation);

                if (doc.Result is not null)
                {
                    results[doc.CallId] = toolResultFactory.Create(doc, invocation);
                }
            }
        }

        return new ProcessedMessage(
            TextContent: message.Content ?? string.Empty,
            Invocations: invocations,
            Results: results
        );
    }
}
