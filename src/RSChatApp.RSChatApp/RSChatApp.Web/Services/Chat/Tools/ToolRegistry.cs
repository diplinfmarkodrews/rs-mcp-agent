using RSChatApp.Shared.Infrastructure.Mcp.MetaData;
using RSChatApp.Web.Services.Chat.Tools.Descriptors;

namespace RSChatApp.Web.Services.Chat.Tools;

public class ToolRegistry
{
    private readonly Dictionary<string, IToolDescriptor> _descriptorsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ToolType, IToolDescriptor> _descriptorsByType = new();
    private readonly IToolDescriptor _unknownDescriptor = new UnknownToolDescriptor();

    public ToolRegistry(IEnumerable<IToolDescriptor> descriptors)
    {
        if (descriptors is null)
            throw new ArgumentNullException(nameof(descriptors));
        
        foreach (var descriptor in descriptors)
        {
            RegisterDescriptor(descriptor);
        }
    }

    public void RegisterDescriptor(IToolDescriptor descriptor)
    {
        _descriptorsByType[descriptor.Type] = descriptor;
        foreach (var name in descriptor.ToolNames)
        {
            _descriptorsByName[name] = descriptor;
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

        return _unknownDescriptor;
    }

    public IToolDescriptor GetDescriptor(ToolType type)
    {
        return _descriptorsByType.GetValueOrDefault(type, _unknownDescriptor);
    }
}
