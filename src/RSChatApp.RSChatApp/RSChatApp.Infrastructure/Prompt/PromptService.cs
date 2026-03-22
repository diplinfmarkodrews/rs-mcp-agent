using System.Text;
using Microsoft.AspNetCore.Hosting;
using RSChatApp.Application.Services;
using RSChatApp.Shared.Infrastructure.Mcp.StaticFileContent.Services;

namespace RSChatApp.Infrastructure.Prompt;


public class PromptService : IPromptService
{
    private readonly IPromptFileStore _promptFileStore;
    private readonly IWebRootFileNameProvider _fileNameProvider;

    public PromptService(IPromptFileStore promptFileStore, IWebRootFileNameProvider fileNameProvider)
    {
        _promptFileStore = promptFileStore;
        _fileNameProvider = fileNameProvider;
    }
    public string GetPrompt(PromptRequest request)
    {
        var result = new StringBuilder();
        switch (request)
        {
            case SystemPromptRequest systemPromptRequest:
                result.Append(_promptFileStore.GetRequired(request.Name));
                if (systemPromptRequest.AddFileNames)
                    result.AppendLine("Here are all document names as reference:")
                        .AppendJoin(", ", _fileNameProvider.GetFileNames("Data"));
                return result.ToString();
            
            case SuggestionPromptRequest suggestionPromptRequest:
            default:
                if (string.IsNullOrWhiteSpace(request.Name) == false)
                    return _promptFileStore.GetRequired(request.Name);
                throw new ArgumentOutOfRangeException(nameof(request));
        }
    }
}
