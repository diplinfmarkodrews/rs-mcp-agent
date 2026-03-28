using RSChatApp.Application.Core.Chat.Dtos;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Core.Chat;

public interface IAiChatClient : IDisposable
{
    IAsyncEnumerable<ChatResponseUpdateDto> GetStreamingResponseAsync(
        AiChatRequest request,
        CancellationToken cancellationToken = default);
}

public record AiChatRequest(string ServiceId, 
    IEnumerable<ChatMessageDto> Messages, 
    IEnumerable<string> ActiveToolNames,
    string? ModelId = null);
    
public record AiChatSettings(string ServiceId, IEnumerable<string> ActiveToolNames, bool IsPrivate, AiChatPromptExecutionSettings PromptExecutionSettings, string? ModelId = null);
