using Microsoft.SemanticKernel;
using RSChatApp.Domain.Chat.ToolCall;
using FunctionCallContent = Microsoft.Extensions.AI.FunctionCallContent;

namespace RSChatApp.Shared.Infrastructure.Mcp.ExtensionAI.ChatClient.Tools;

public class ToolInvocationFactory
{
    private readonly ToolRegistry _registry;

    public ToolInvocationFactory(ToolRegistry registry)
    {
        _registry = registry;
    }
    // Extensions.AI
    public ToolInvocation Create(FunctionCallContent fcc)
    {
        var descriptor = _registry.GetDescriptor(fcc.Name);
        var parameters = fcc.Arguments?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                         ?? new Dictionary<string, object?>();
        var result = new ToolInvocation(
            CallId: fcc.CallId,
            Type: descriptor.Type,
            ResultContentType: descriptor.ResultContentType,
            RawName: fcc.Name,
            DisplayName: descriptor.GetDisplayName(parameters),
            Parameters: parameters,
            Metadata: descriptor.ExtractMetadata(parameters),
            Permissions: descriptor.GetPermissions(parameters),
            UiHints: descriptor.GetUiHints(parameters)
        );
        return result;
    }
    // Semantic Kernel 
    public ToolInvocation Create(KernelFunction kernelFunction)
    {
        var descriptor = _registry.GetDescriptor(kernelFunction.Name);
        var parameters = kernelFunction.AdditionalProperties;
        var result = new ToolInvocation(
            CallId: Guid.NewGuid().ToString(), // Generate a new CallId since we don't have one in this context
            Type: descriptor.Type,
            ResultContentType: descriptor.ResultContentType,
            RawName: kernelFunction.Name,
            DisplayName: descriptor.GetDisplayName(parameters),
            Parameters: parameters,
            Metadata: descriptor.ExtractMetadata(parameters),
            Permissions: descriptor.GetPermissions(parameters),
            UiHints: descriptor.GetUiHints(parameters)
        );
        return result;
    }

    // From persisted ToolCallDocument
    public ToolInvocation Create(ToolCallDocument doc)
    {
        var descriptor = _registry.GetDescriptor(doc.ToolName);
        var parameters = doc.Arguments
            .ToDictionary<KeyValuePair<string, object>, string, object?>(kvp => kvp.Key, kvp => kvp.Value);
        return new ToolInvocation(
            CallId: doc.CallId,
            Type: descriptor.Type,
            ResultContentType: descriptor.ResultContentType,
            RawName: doc.ToolName,
            DisplayName: descriptor.GetDisplayName(parameters),
            Parameters: parameters,
            Metadata: descriptor.ExtractMetadata(parameters),
            Permissions: descriptor.GetPermissions(parameters),
            UiHints: descriptor.GetUiHints(parameters)
        );
    }
}