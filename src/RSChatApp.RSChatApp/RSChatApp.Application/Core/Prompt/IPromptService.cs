namespace RSChatApp.Application.Services;

public interface IPromptService
{
    string GetPrompt(PromptRequest request);
}
public record PromptRequest(string Name);

public record SystemPromptRequest(bool AddFileNames) 
    : PromptRequest("SystemPrompt");

public record SuggestionPromptRequest()
    : PromptRequest("SuggestionPrompt");