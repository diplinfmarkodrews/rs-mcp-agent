using RSChatApp.Application.Core.Message.Dtos;

namespace RSChatApp.Application.Features.GetConversation;

public static class GetConversationHandler
{
    public static async Task<ConversationDto?> Handle(
        GetConversationQuery query,
        IReadOnlyEventStore session,
        CancellationToken ct)
    {
        var result = await session.QueryFirstOrDefaultAsync<ConversationDto>(
            q => q.Where(c => c.Id == query.SessionId),
            ct).ConfigureAwait(false);

        if (result is not null && result.UserId != query.UserId)
        {
            throw new DomainException("Forbidden");
        }

        return result;
    }
}
