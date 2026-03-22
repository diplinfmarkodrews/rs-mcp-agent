using Marten;
using RSChatApp.Application.Core.Message.Dtos;
using RSChatApp.Application.Features.Message.Events;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Services;

public class PromptBuilder(IQuerySession session, IPromptService promptService) : IPromptBuilder
{
    
    public async Task<IReadOnlyList<ChatTurn>> BuildAsync(Guid sessionId, CancellationToken ct)
    {
        var messages = await session.Query<MessageDto>()
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.SentAt)
            // .Take(20)
            .ToListAsync(ct);

        var history = messages.Select(msg => new ChatTurn(
            msg.Role == ChatRole.User ? ChatRole.User : ChatRole.Assistant,
            msg.Content));

        var result = new List<ChatTurn>
        {
            new(ChatRole.System, promptService.GetPrompt(new SystemPromptRequest(AddFileNames: true)))
        };
        
        result.AddRange(history);
        
        return result;
    }
}
