using Vogen;

namespace RSChatApp.Domain.ValueObjects;

[ValueObject<int>]
public readonly partial struct ToolCallStatus
{
    public static readonly ToolCallStatus Requested = From(1);
    public static readonly ToolCallStatus AwaitingCallConfirmation = From(2);
    public static readonly ToolCallStatus Executing = From(3);
    public static readonly ToolCallStatus AwaitingResultConfirmation = From(4);
    public static readonly ToolCallStatus Completed = From(5);
    public static readonly ToolCallStatus CompletedRedacted = From(6);
    public static readonly ToolCallStatus Rejected = From(7);
    public static readonly ToolCallStatus Failed = From(8);
    public static readonly ToolCallStatus Expired = From(9);

    private static Validation Validate(int value)
        => value is >= 1 and <= 9
            ? Validation.Ok
            : Validation.Invalid("ToolCallStatus must be between 1 and 9.");
}
