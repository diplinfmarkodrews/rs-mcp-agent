using RSChatApp.Application.Core.Chat;
using RSChatApp.Application.Core.Chat.Dtos;
using RSChatApp.Application.Services;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Core.Prompt;

public class PromptBuilder(IChatMessageQuery chatMessageQuery, IPromptService promptService) : IPromptBuilder
{
    public async Task<IReadOnlyList<ChatMessageDto>> BuildAsync(Guid sessionId, CancellationToken ct)
    {
        var history = await chatMessageQuery.GetBySessionAsync(sessionId, ct);

        var result = new List<ChatMessageDto>
        {
            new(ChatRole.System, promptService.GetPrompt(new SystemPromptRequest(AddFileNames: true)))
        };
        
        result.AddRange(history);
        
        return result;
    }
}
