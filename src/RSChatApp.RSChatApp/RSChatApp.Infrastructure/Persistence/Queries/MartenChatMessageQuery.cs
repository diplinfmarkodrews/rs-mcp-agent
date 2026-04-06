using RSChatApp.Application.Core.Chat;
using RSChatApp.Application.Core.Chat.Dtos;
using RSChatApp.Application.Core.Message.Dtos;
using RSChatApp.Domain.Chat.ToolCall;

namespace RSChatApp.Infrastructure.Persistence.Queries;

public class MartenChatMessageQuery(IQuerySession session) : IChatMessageQuery
{
    public async Task<IReadOnlyList<ChatMessageDto>> GetBySessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var messages = await session.Query<MessageDto>()
            .Where(m => m.SessionId == sessionId && m.IsComplete)
            .OrderBy(m => m.SentAt)
            .ToListAsync(ct);

        var toolCalls = await session.Query<ToolCallDocument>()
            .Where(tc => tc.SessionId == sessionId)
            .ToListAsync(ct);

        return messages.ToChatMessageDtos(toolCalls);
    }
}
