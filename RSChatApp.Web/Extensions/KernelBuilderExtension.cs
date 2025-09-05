using Microsoft.SemanticKernel;

namespace RSChatApp.Web.Extensions;

public static class KernelBuilderExtension
{
    public static IKernelBuilder AddChatClients(this IKernelBuilder kernelBuilder)
    {
        return kernelBuilder;
    }
}