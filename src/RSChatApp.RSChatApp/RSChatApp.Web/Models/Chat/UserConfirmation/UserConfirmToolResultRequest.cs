using RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient.Tools;

namespace RSChatApp.Web.Models.Chat.UserConfirmation;

public record UserConfirmToolResultRequest(string ToolName, ToolInvocation ToolInvocation, ToolResult ToolResult);
