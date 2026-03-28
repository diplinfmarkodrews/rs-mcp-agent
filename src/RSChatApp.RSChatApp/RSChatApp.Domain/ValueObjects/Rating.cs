using Vogen;

namespace RSChatApp.Domain.ValueObjects;

[ValueObject<int>]
public readonly partial struct Rating
{
    private static Validation Validate(int value)
        => value is >= 1 and <= 5 ? Validation.Ok : Validation.Invalid("Rating must be between 1 and 5.");
}

