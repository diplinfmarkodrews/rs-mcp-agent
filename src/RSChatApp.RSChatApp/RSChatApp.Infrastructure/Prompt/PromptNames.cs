namespace RSChatApp.Infrastructure.Prompt;

public static class PromptNames
{
    public const string SystemPrompt = "SystemPrompt";
    public const string SuggestionPrompt = "SuggestionPrompt";

    public static readonly string[] Required =
    [
        SystemPrompt,
        SuggestionPrompt,
    ];
}
