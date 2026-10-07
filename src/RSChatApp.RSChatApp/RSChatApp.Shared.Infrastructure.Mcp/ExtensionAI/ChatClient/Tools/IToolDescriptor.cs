using RSChatApp.Shared.Infrastructure.Mcp.MetaData;

namespace RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient.Tools;

public interface IToolDescriptor
{
    ToolType Type { get; }
    ResultContentType ResultContentType { get; }
    string GetDisplayName(IReadOnlyDictionary<string, object?> parameters);
    ToolPermissions GetPermissions(IReadOnlyDictionary<string, object?> parameters);
    ToolMetadata ExtractMetadata(IReadOnlyDictionary<string, object?> parameters);
    ToolUiHints GetUiHints(IReadOnlyDictionary<string, object?> parameters);
    ToolUserConfirmation GetUserConfirmation(string? funtionName = null);
    string GetIconSvg();
    IEnumerable<string> ToolNames { get; }
    string GetColorClass();
}
