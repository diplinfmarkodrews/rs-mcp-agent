using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using RSChatApp.Application.Core.Chat;
using RSChatApp.Application.Core.Chat.Dtos;
using RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient.Tools;

namespace RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient;

public sealed class AiChatClientFacade(IServiceProvider serviceProvider, Kernel kernel, IToolCollectionService toolCollectionService) : IAiChatClient
{
    public IAsyncEnumerable<ChatMessageUpdateDto> GetStreamingResponseAsync(
        AiChatRequest request,
        CancellationToken cancellationToken = default)
    {
        using var chatClient = serviceProvider.GetKeyedService<IChatClient>(request.Settings.ServiceId);

        IAiChatClient inner = chatClient is not null
            ? new ExtensionsAiChatClient(chatClient, toolCollectionService)
            : new SemanticKernelChatClient(kernel);

        return inner.GetStreamingResponseAsync(request, cancellationToken);
    }
    
}
