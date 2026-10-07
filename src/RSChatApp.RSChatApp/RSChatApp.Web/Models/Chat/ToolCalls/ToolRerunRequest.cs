using RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient.Tools;

namespace RSChatApp.Web.Models.Chat.ToolCalls;

public record ToolRerunRequest(
    string OriginalCallId,
    ToolInvocation Invocation,
    string MessageId
);
