namespace RSChatApp.Shared.Infrastructure.Mcp.Browser.Extensions;

public static class MemoryCacheExtension
{
    public static string CreatBrowserInstanceCacheKey(this string sessionId)
    {
        return $"BrowserInstance:{sessionId}";
    }
}