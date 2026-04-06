namespace RSChatApp.Domain.ValueObjects;

public class GaveUpReasons : SmartEnum<GaveUpReasons>
{
    public static readonly GaveUpReasons NotSet = new("NOT_SET", 0);
    public static readonly GaveUpReasons LlmError = new("LLM_ERROR", 1);
    public static readonly GaveUpReasons Timeout = new("LLM_TIMEOUT", 2);
    public static readonly GaveUpReasons MaxRetriesExceeded = new("MAX_RETRIES_EXCEEDED", 3);
    public static readonly GaveUpReasons SessionDeleted = new("SESSION_DELETED", 4);
    public static readonly GaveUpReasons ToolCallRejected = new("TOOL_CALL_REJECTED", 5);
    public static readonly GaveUpReasons ToolResultRejected = new("TOOL_RESULT_REJECTED", 6);
    public static readonly GaveUpReasons Cancelled = new("CANCELLED", 7);
    
    protected GaveUpReasons(string name, int value) : base(name, value) { }
}