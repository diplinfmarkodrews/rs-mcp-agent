using RSChatApp.Web.Models.Chat.ToolCalls;
using RSChatApp.Web.Services.Chat.Tools.Descriptors;

namespace RSChatApp.Web.Services.Chat.Tools;

public class ToolRegistry
{
    private readonly Dictionary<string, IToolDescriptor> _descriptorsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ToolType, IToolDescriptor> _descriptorsByType = new();
    private readonly IToolDescriptor _unknownDescriptor = new UnknownToolDescriptor();

    public ToolRegistry()
    {
        RegisterDescriptor(new SearchToolDescriptor(), 
            "Search", "SearchAsync", "search");
        
        RegisterDescriptor(new TerminalToolDescriptor(), 
            "RsMcpServer_execute_command");
        
        RegisterDescriptor(new BrowserToolDescriptor(), 
            "BrowserTool", "BrowserTool_executejavascript", "executeScript", "browser");
    }

    public void RegisterDescriptor(IToolDescriptor descriptor, params string[] toolNames)
    {
        _descriptorsByType[descriptor.Type] = descriptor;
        
        foreach (var name in toolNames)
        {
            _descriptorsByName[name] = descriptor;
            _descriptorsByName[NormalizeToolName(name)] = descriptor;
        }
    }

    public IToolDescriptor GetDescriptor(string? toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return _unknownDescriptor;
        }

        if (_descriptorsByName.TryGetValue(toolName, out var descriptor))
        {
            return descriptor;
        }

        var normalized = NormalizeToolName(toolName);
        if (_descriptorsByName.TryGetValue(normalized, out descriptor))
        {
            return descriptor;
        }

        return _unknownDescriptor;
    }

    public IToolDescriptor GetDescriptor(ToolType type)
    {
        return _descriptorsByType.GetValueOrDefault(type, _unknownDescriptor);
    }

    private static string NormalizeToolName(string name)
    {
        // Remove namespace prefixes and async suffix
        var parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var lastPart = parts[^1];
        
        if (lastPart.EndsWith("Async", StringComparison.OrdinalIgnoreCase))
        {
            lastPart = lastPart[..^5];
        }

        // Remove all non-alphanumeric and lowercase
        return new string(lastPart.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }
}
