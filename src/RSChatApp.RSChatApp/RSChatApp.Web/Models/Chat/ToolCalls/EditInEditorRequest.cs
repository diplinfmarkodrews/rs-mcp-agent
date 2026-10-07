using RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient.Tools;

namespace RSChatApp.Web.Models.Chat.ToolCalls;

public record EditInEditorRequest(
    ToolResult Result,
    string? Filename,
    string Content
);
