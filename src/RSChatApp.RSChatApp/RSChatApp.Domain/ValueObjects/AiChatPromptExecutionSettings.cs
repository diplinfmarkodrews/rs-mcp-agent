namespace RSChatApp.Domain.ValueObjects;

public record AiChatPromptExecutionSettings(
    float Temperature,
    float TopP,
    float FrequencyPenalty,
    float PresencePenalty,
    bool AllowMultipleToolCalls);

