using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace RSChatApp.Web.Services.Prompt;

public interface IPromptService
{
    string GetPrompt(PromptRequest request);
}
public class PromptService : IPromptService
{
    private readonly IPromptFileStore _promptFileStore;
    private readonly IWebHostEnvironment _environment;

    public PromptService(IPromptFileStore promptFileStore, IWebHostEnvironment environment)
    {
        _promptFileStore = promptFileStore;
        _environment = environment;
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
                        .AppendJoin(", ", ReadFileNames());
                return result.ToString();
            
            case SuggestionPromptRequest suggestionPromptRequest:
            default:
                if (string.IsNullOrWhiteSpace(request.Name) == false)
                    return _promptFileStore.GetRequired(request.Name);
                throw new ArgumentOutOfRangeException(nameof(request));
        }
    }

    private IEnumerable<string> ReadFileNames()
    {
        return Directory.EnumerateFiles(Path.Combine(_environment.WebRootPath, "Data"))
            .Select(p => Path.GetFileName(p));
    }
}

public record PromptRequest(string Name);

public record SystemPromptRequest(bool AddFileNames) 
    : PromptRequest("SystemPrompt");

public record SuggestionPromptRequest()
    : PromptRequest("SuggestionPrompt");