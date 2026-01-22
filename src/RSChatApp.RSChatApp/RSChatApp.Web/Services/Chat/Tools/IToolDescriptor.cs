using RSChatApp.Web.Models.Chat.ToolCalls;

namespace RSChatApp.Web.Services.Chat.Tools;

public interface IToolDescriptor
{
    ToolType Type { get; }
    string GetDisplayName(IReadOnlyDictionary<string, object?> parameters);
    ToolPermissions GetPermissions(IReadOnlyDictionary<string, object?> parameters);
    ToolMetadata ExtractMetadata(IReadOnlyDictionary<string, object?> parameters);
    string GetIconSvg();
    string GetColorClass();
}
