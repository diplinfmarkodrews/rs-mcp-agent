namespace RSChatApp.Web.Models.Chat.UserConfirmation;

public class UserConfirmToolCallRequest
{
    public string ToolName { get; set; } = string.Empty;
    public IDictionary<string, object?> Arguments { get; set; } = new Dictionary<string, object?>();
}