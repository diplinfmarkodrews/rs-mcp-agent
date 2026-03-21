using Microsoft.SemanticKernel;
using RSChatApp.Web.Models.Chat.ToolCalls;
using FunctionCallContent = Microsoft.Extensions.AI.FunctionCallContent;

namespace RSChatApp.Web.Services.Chat.Tools;

public class ToolInvocationFactory
{
    private readonly ToolRegistry _registry;

    public ToolInvocationFactory(ToolRegistry registry)
    {
        _registry = registry;
    }
    public ToolInvocation Create(FunctionCallContent fcc)
    {
        var descriptor = _registry.GetDescriptor(fcc.Name);
        var parameters = fcc.Arguments?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                         ?? new Dictionary<string, object?>();
        var result = new ToolInvocation(
            CallId: fcc.CallId,
            Type: descriptor.Type,
            RawName: fcc.Name,
            DisplayName: descriptor.GetDisplayName(parameters),
            Parameters: parameters,
            Metadata: descriptor.ExtractMetadata(parameters),
            Permissions: descriptor.GetPermissions(parameters),
            UiHints: descriptor.GetUiHints(parameters)
        );
        return result;
    }

    public ToolInvocation Create(KernelFunction kernelFunction)
    {
        var descriptor = _registry.GetDescriptor(kernelFunction.Name);
        var parameters = kernelFunction.AdditionalProperties;
        var result = new ToolInvocation(
            CallId: Guid.NewGuid().ToString(), // Generate a new CallId since we don't have one in this context
            Type: descriptor.Type,
            RawName: kernelFunction.Name,
            DisplayName: descriptor.GetDisplayName(parameters),
            Parameters: parameters,
            Metadata: descriptor.ExtractMetadata(parameters),
            Permissions: descriptor.GetPermissions(parameters),
            UiHints: descriptor.GetUiHints(parameters)
        );
        return result;
    }
}