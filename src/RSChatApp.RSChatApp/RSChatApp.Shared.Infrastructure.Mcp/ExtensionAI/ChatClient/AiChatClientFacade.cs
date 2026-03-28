using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using RSChatApp.Application.Core.Chat;
using RSChatApp.Application.Core.Chat.Dtos;

namespace RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient;

public sealed class AiChatClientFacade(IServiceProvider serviceProvider, Kernel kernel) : IAiChatClient
{
    public IAsyncEnumerable<ChatResponseUpdateDto> GetStreamingResponseAsync(
        AiChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var chatClient = serviceProvider.GetKeyedService<IChatClient>(request.ServiceId);

        IAiChatClient inner = chatClient is not null
            ? new ExtensionsAiChatClient(chatClient)
            : new SemanticKernelChatClient(kernel);

        return inner.GetStreamingResponseAsync(request, cancellationToken);
    }

    public void Dispose() { }
}
