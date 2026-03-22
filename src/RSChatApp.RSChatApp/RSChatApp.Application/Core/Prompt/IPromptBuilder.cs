using RSChatApp.Application.Features.Message.Events;

namespace RSChatApp.Application.Services;

public interface IPromptBuilder
{
    Task<IReadOnlyList<ChatTurn>> BuildAsync(Guid sessionId, CancellationToken ct);
}
