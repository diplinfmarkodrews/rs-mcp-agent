using Microsoft.Extensions.AI;
using RSChatApp.Application.Core.Chat;
using RSChatApp.Application.Core.Chat.Dtos;
using RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.Processing;

namespace RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient;

internal sealed class ExtensionsAiChatClient(IChatClient inner) : IAiChatClient
{
    public async IAsyncEnumerable<ChatResponseUpdateDto> GetStreamingResponseAsync(
        AiChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = request.Messages
            .Select(m => new ChatMessage(ChatRoleMapper.ToExtensionsAI(m.role), m.content))
            .ToList()
            .NormalizeMessagesForApi();

        await foreach (var update in inner.GetStreamingResponseAsync(messages, cancellationToken: cancellationToken))
        {
            var role = update.Role?.Value;
            var finishReason = update.FinishReason?.Value;

            foreach (var content in update.Contents)
            {
                yield return content switch
                {
                    TextContent text => new ChatResponseUpdateDto(
                        TextDelta: text.Text,
                        Role: role,
                        FinishReason: finishReason),

                    FunctionCallContent call => new ChatResponseUpdateDto(
                        Role: role,
                        ToolCall: new ToolCallInfo(call.Name, call.Arguments?.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value ?? (object)string.Empty) ?? new Dictionary<string, object>())),

                    FunctionResultContent result => new ChatResponseUpdateDto(
                        Role: role,
                        ToolResult: new ToolResultInfo(result.CallId, result.Result?.ToString() ?? string.Empty)),

                    _ => new ChatResponseUpdateDto(Role: role, FinishReason: finishReason),
                };
            }
        }
    }

    public void Dispose() => inner.Dispose();
}
