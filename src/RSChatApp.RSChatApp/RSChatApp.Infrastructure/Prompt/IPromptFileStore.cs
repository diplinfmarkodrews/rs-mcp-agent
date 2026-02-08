namespace RSChatApp.Web.Services.Prompt;

public interface IPromptFileStore
{
    string GetRequired(string name);
    bool TryGet(string name, out string? prompt);
    IReadOnlyDictionary<string, string> GetAll();
}
