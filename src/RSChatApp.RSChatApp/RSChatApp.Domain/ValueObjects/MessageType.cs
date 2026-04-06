using Vogen;

namespace RSChatApp.Domain.ValueObjects;

[ValueObject<int>]
public readonly partial struct MessageType
{
    public static readonly MessageType TextDelta = From(1);
    public static readonly MessageType TextFull = From(2);
    public static readonly MessageType ToolCall = From(4);
    public static readonly MessageType ToolResult = From(8);

    private const int AllFlags = 1 | 2 | 4 | 8;

    public bool Has(MessageType flag) => (Value & flag.Value) != 0;

    public MessageType With(MessageType flag) => From(Value | flag.Value);

    private static Validation Validate(int value)
        => value > 0 && (value & ~AllFlags) == 0
            ? Validation.Ok
            : Validation.Invalid("MessageType must be a valid combination of TextDelta (1), TextFull (2), ToolCall (4), ToolResult (8).");
}

