using Vogen;

[assembly: VogenDefaults(
    staticAbstractsGeneration: StaticAbstractsGeneration.MostCommon | StaticAbstractsGeneration.InstanceMethodsAndProperties)]

namespace RSChatApp.Domain.ValueObjects;
[ValueObject<string>]
public readonly partial struct UserId
{
    private static Validation Validate(string value)
        => !string.IsNullOrWhiteSpace(value) ? Validation.Ok : Validation.Invalid("UserId cannot be empty.");
}