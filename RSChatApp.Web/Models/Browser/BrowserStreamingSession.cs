using Microsoft.Playwright;

namespace RSChatApp.Web.Models.Browser;

//Disposal by Service
public class BrowserStreamingSession
{
    public required string SessionId { get; init; }
    public required IBrowserContext Context { get; init; }
    public required IPage Page { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset LastActivity { get; init; }
    public required bool IsStreaming { get; init; }
}
