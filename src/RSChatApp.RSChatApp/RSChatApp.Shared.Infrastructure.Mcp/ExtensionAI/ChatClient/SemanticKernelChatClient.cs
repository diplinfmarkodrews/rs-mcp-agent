using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using RSChatApp.Application.Core.Chat;
using RSChatApp.Application.Core.Chat.Dtos;

namespace RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient;

internal sealed class SemanticKernelChatClient(Kernel kernel) : IAiChatClient
{
    public async IAsyncEnumerable<ChatMessageUpdateDto> GetStreamingResponseAsync(
        AiChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatHistory = new ChatHistory();
        foreach (var m in request.Message.Messages)
            chatHistory.AddMessage(m.Role.ToSemanticKernel(), m.Content);

        var allowedFunctions = kernel.Plugins
            .SelectMany(p => p)
            .Where(f => !request.Settings.ActiveToolNames.Any() || request.Settings.ActiveToolNames.Contains(f.Name))
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
                    StreamingTextContent text => new ChatMessageUpdateDto(
                        TextDelta: text.Text,
                        Role: role.ToChatRole()),

                    StreamingFunctionCallUpdateContent call when call.Name is not null
                        => new ChatMessageUpdateDto(
                            Role: role.ToChatRole(),
                            ToolCall: new ToolCallInfo(call.Name, ParseArguments(call.Arguments))),
                    // SK doesn't expose FunctionCallUpdate results in stream api, meh
                    _ => new ChatMessageUpdateDto(Role: role.ToChatRole()),
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
