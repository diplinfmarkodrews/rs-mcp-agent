using Vogen;

namespace RSChatApp.Domain.ValueObjects;

[ValueObject<float>]
public readonly partial struct Temperature
{
    private static Validation Validate(float value)
        => value is >= 0f and <= 2f ? Validation.Ok : Validation.Invalid("Temperature must be between 0 and 2.");
}

[ValueObject<float>]
public readonly partial struct TopP
{
    private static Validation Validate(float value)
        => value is >= 0f and <= 1f ? Validation.Ok : Validation.Invalid("TopP must be between 0 and 1.");
}

[ValueObject<float>]
public readonly partial struct FrequencyPenalty
{
    private static Validation Validate(float value)
        => value is >= -2f and <= 2f ? Validation.Ok : Validation.Invalid("FrequencyPenalty must be between -2 and 2.");
}

[ValueObject<float>]
public readonly partial struct PresencePenalty
{
    private static Validation Validate(float value)
        => value is >= -2f and <= 2f ? Validation.Ok : Validation.Invalid("PresencePenalty must be between -2 and 2.");
}

public record AiChatPromptExecutionSettings(
    Temperature Temperature,
    TopP TopP,
    FrequencyPenalty FrequencyPenalty,
    PresencePenalty PresencePenalty,
    bool AllowMultipleToolCalls)
{
    public static AiChatPromptExecutionSettings Default
        => new
        (
            Temperature.From(0.7f),
            TopP.From(1f),
            FrequencyPenalty.From(0f),
            PresencePenalty.From(0f),
            true
        );
};
