using Microsoft.Playwright;
using RSChatApp.Mcp.Browser.Interfaces;

namespace RSChatApp.Web.Models.Browser;

//Disposal by Service
public class BrowserStreamingSession
{
    public required string StreamSessionId { get; init; }
    public required IBrowserInstance BrowserInstance { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset LastActivity { get; set; }
    public required bool IsStreaming { get; set; }
}
