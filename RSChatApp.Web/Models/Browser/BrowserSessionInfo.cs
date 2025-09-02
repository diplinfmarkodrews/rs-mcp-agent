namespace RSChatApp.Web.Models.Browser;

public class BrowserSessionInfo
{
    public required string SessionId { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset LastActivity { get; init; }
    public required bool IsStreaming { get; init; }
}
