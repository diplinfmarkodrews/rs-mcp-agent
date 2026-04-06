using RSChatApp.Application.Core.Chat.Dtos;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Core.Chat;

public interface IAiChatClient
{
    IAsyncEnumerable<ChatMessageUpdateDto> GetStreamingResponseAsync(
        AiChatRequest request,
        CancellationToken cancellationToken = default);
}

public record AiChatRequestMessage(List<ChatMessageDto> Messages);


public record AiChatRequest(
    Guid SessionId,
    AiChatRequestMessage Message,
    AiChatSettings Settings);
  

public record AiChatSettings(
    string ServiceId, 
    IEnumerable<string> ActiveToolNames, 
    bool IsPrivate, 
    bool IsLocal,
    AiChatPromptExecutionSettings PromptExecutionSettings, 
    string? ModelId = null);