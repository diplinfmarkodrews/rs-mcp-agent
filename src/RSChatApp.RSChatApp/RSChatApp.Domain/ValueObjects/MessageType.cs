using Vogen;

namespace RSChatApp.Domain.ValueObjects;

[ValueObject<int>]
public readonly partial struct MessageType
{
    public static readonly MessageType Text = From(1);
    public static readonly MessageType ToolCall = From(2);
    public static readonly MessageType ToolResult = From(3);

    private static Validation Validate(int value)
        => value is >= 1 and <= 3 ? Validation.Ok : Validation.Invalid("MessageType must be Text (1), ToolCall (2), or ToolResult (3).");
}

