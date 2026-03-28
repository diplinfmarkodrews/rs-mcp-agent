using RSChatApp.Application.Core.Chat.Dtos;

namespace RSChatApp.Application.Core.Prompt;

public interface IPromptBuilder
{
    Task<IReadOnlyList<ChatMessageDto>> BuildAsync(Guid sessionId, CancellationToken ct);
}
