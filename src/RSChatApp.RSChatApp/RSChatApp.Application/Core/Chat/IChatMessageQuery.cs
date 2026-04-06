using RSChatApp.Application.Core.Chat.Dtos;

namespace RSChatApp.Application.Core.Chat;

public interface IChatMessageQuery
{
    Task<IReadOnlyList<ChatMessageDto>> GetBySessionAsync(Guid sessionId, CancellationToken ct = default);
}

