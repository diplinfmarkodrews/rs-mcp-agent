namespace RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient.Tools;
public record ProcessedMessage(
    string TextContent,
    List<ToolInvocation> Invocations,
    Dictionary<string, ToolResult> Results
);
