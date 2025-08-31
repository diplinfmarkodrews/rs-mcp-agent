using Microsoft.SemanticKernel;

namespace RSChatApp.Mcp;

public class McpClientFactory
{
    private readonly IKernelBuilder _kernelBuilder;

    public McpClientFactory(IKernelBuilder kernelBuilder)
    {
        _kernelBuilder = kernelBuilder;    
    }
    
}