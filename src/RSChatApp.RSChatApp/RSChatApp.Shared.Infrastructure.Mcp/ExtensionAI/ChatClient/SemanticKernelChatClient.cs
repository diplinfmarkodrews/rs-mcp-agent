using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using RSChatApp.Application.Core.Chat;
using RSChatApp.Application.Core.Chat.Dtos;

namespace RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient;

internal sealed class SemanticKernelChatClient(Kernel kernel) : IAiChatClient
{
    public async IAsyncEnumerable<ChatResponseUpdateDto> GetStreamingResponseAsync(
        AiChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatHistory = new ChatHistory();
        foreach (var m in request.Messages)
            chatHistory.AddMessage(m.role.ToSemanticKernel(), m.content);

        var allowedFunctions = kernel.Plugins
            .SelectMany(p => p)
            .Where(f => !request.ActiveToolNames.Any() || request.ActiveToolNames.Contains(f.Name))
            .ToList();

        var settings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(allowedFunctions),
        };

        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

        await foreach (var chunk in chatCompletion.GetStreamingChatMessageContentsAsync(
                           chatHistory, settings, kernel, cancellationToken))
        {
            var role = chunk.Role?.ToString();

            foreach (var item in chunk.Items)
            {
                yield return item switch
                {
                    StreamingTextContent text => new ChatResponseUpdateDto(
                        TextDelta: text.Text,
                        Role: role),

                    StreamingFunctionCallUpdateContent call when call.Name is not null => new ChatResponseUpdateDto(
                        Role: role,
                        ToolCall: new ToolCallInfo(call.Name, ParseArguments(call.Arguments))),
                    
                    _ => new ChatResponseUpdateDto(Role: role),
                };
            }
        }
    }

    public void Dispose() { }

    private static Dictionary<string, object> ParseArguments(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, object>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => (object)p.Value.Clone());
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
}
