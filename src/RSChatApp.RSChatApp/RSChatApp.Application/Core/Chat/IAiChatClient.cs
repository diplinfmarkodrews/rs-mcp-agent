using RSChatApp.Application.Core.Chat.Dtos;

namespace RSChatApp.Application.Core.Chat;

public interface IAiChatClient : IDisposable
{
    IAsyncEnumerable<ChatResponseUpdateDto> GetStreamingResponseAsync(
        string serviceId,
        IEnumerable<ChatMessageDto> messages,
        string? modelId = null,
        CancellationToken cancellationToken = default);
}
