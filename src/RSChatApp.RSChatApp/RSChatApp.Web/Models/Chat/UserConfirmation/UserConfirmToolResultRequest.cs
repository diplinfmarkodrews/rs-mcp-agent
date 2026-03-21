using RSChatApp.Web.Models.Chat.ToolCalls;

namespace RSChatApp.Web.Models.Chat.UserConfirmation;

public record UserConfirmToolResultRequest(string ToolName, ToolInvocation ToolInvocation, ToolResult ToolResult);
