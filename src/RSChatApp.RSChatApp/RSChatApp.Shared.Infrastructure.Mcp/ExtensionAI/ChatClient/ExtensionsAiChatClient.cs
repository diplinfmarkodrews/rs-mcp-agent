using Microsoft.Extensions.AI;
using RSChatApp.Application.Core.Chat;
using RSChatApp.Application.Core.Chat.Dtos;
using RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.Processing;

namespace RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient;

internal sealed class ExtensionsAiChatClient(IChatClient inner) : IAiChatClient
{
    public async IAsyncEnumerable<ChatMessageUpdateDto> GetStreamingResponseAsync(
        AiChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = request.Message.Messages
            .ToChatMessageList();
            // .NormalizeMessagesForApi();

        await foreach (var update in inner.GetStreamingResponseAsync(messages, cancellationToken: cancellationToken))
        {
            var role = update.Role?.Value;
            var finishReason = update.FinishReason?.Value;

            foreach (var content in update.Contents)
            {
                yield return content switch
                {
                    TextContent text => new ChatMessageUpdateDto(
                        TextDelta: text.Text,
                        Role: role.ToChatRole(),
                        FinishReason: finishReason),

                    FunctionCallContent call => new ChatMessageUpdateDto(
                        Role: role.ToChatRole(),
                        ToolCall: new ToolCallInfo(call.Name, call.Arguments?.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value ?? (object)string.Empty) ?? new Dictionary<string, object>())),

                    FunctionResultContent result => new ChatMessageUpdateDto(
                        Role: role.ToChatRole(),
                        ToolResult: new ToolResultInfo(result.CallId, result.Result)),

                    _ => new ChatMessageUpdateDto(Role: role.ToChatRole(), FinishReason: finishReason),
                };
            }
        }
    }

    public void Dispose() => inner.Dispose();
}
