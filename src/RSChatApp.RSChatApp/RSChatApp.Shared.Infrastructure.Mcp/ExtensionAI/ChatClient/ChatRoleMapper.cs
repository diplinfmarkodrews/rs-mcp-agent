using Microsoft.SemanticKernel.ChatCompletion;
using MsAI = Microsoft.Extensions.AI;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient;

internal static class ChatRoleMapper
{
    internal static MsAI.ChatRole ToExtensionsAI(this ChatRole role) 
        => role.Name switch
        {
            nameof(ChatRole.User)      => MsAI.ChatRole.User,
            nameof(ChatRole.Assistant) => MsAI.ChatRole.Assistant,
            nameof(ChatRole.System)    => MsAI.ChatRole.System,
            nameof(ChatRole.Tool)      => MsAI.ChatRole.Tool,
            _                          => MsAI.ChatRole.User,
        };

    internal static AuthorRole ToSemanticKernel(this ChatRole role) 
        => role.Name switch
        {
            nameof(ChatRole.User)      => AuthorRole.User,
            nameof(ChatRole.Assistant) => AuthorRole.Assistant,
            nameof(ChatRole.System)    => AuthorRole.System,
            nameof(ChatRole.Tool)      => AuthorRole.Tool,
            _                          => AuthorRole.User,
        };
    
    internal static ChatRole ToChatRole(this string? role)
        => ChatRole.FromName(role ?? string.Empty);
    
}
