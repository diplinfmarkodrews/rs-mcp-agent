using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient;

public interface IChatClientFactory
{
    IChatClient Create(string serviceKey);
}

public class ChatClientFactory : IChatClientFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ChatClientFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public IChatClient Create(string serviceKey)
    {
        return _serviceProvider.GetRequiredKeyedService<IChatClient>(serviceKey);
    }
}