using System.Text;
using Microsoft.Extensions.Options;

namespace RSChatApp.Web.Services.Prompt;

public interface IPromptService
{
    string GetPrompt(PromptRequest request);
}
public class PromptService : IPromptService
{
    private readonly IPromptStore _promptStore;
    private readonly IWebHostEnvironment _environment;

    public PromptService(IPromptStore promptStore, IWebHostEnvironment environment)
    {
        _promptStore = promptStore;
        _environment = environment;
    }
    public string GetPrompt(PromptRequest request)
    {
        var result = new StringBuilder();
        switch (request)
        {
            case SystemPromptRequest systemPromptRequest:
                result.Append(_promptStore.GetRequired(request.Name));
                if (systemPromptRequest.AddFileNames)
                    result.AppendLine("Here are all document names as reference:")
                        .AppendJoin(", ", ReadFileNames());
                return result.ToString();
            
            case SuggestionPromptRequest suggestionPromptRequest:
            default:
                if (string.IsNullOrWhiteSpace(request.Name) == false)
                    return _promptStore.GetRequired(request.Name);
                throw new ArgumentOutOfRangeException(nameof(request));
        }
    }

    private IEnumerable<string> ReadFileNames()
    {
        return Directory.EnumerateFiles(Path.Combine(_environment.WebRootPath, "Data"));
    }
}

public record PromptRequest(string Name);

public record SystemPromptRequest(bool AddFileNames) 
    : PromptRequest("SystemPrompt");

public record SuggestionPromptRequest()
    : PromptRequest("SuggestionPrompt");