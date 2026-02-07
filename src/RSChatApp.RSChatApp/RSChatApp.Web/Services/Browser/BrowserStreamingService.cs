using Microsoft.AspNetCore.SignalR;
using RSChatApp.Web.Hubs;
using System.Collections.Concurrent;
using System.Text.Json;
using RSChatApp.Shared.Infrastructure.Mcp.Browser.Interfaces;

namespace RSChatApp.Web.Services.Browser;

/// <summary>
/// Browser streaming service that handles WebSocket-based live browser streaming
/// </summary>
public interface IBrowserStreamingService
{
    Task<string> CreateStreamingSessionAsync(string? initialUrl = null);
    Task CloseStreamingSessionAsync(string sessionId);
    Task NavigateAsync(string sessionId, string url);
    Task HandleInteractionAsync(string sessionId, BrowserInteraction interaction);
    Task StartStreamingAsync(string sessionId);
    Task StopStreamingAsync(string sessionId);
    Task<BrowserSessionInfo?> GetSessionInfoAsync(string sessionId);
}

public class BrowserStreamingService : IBrowserStreamingService, IDisposable
{
    private readonly IBrowserInstanceProvider _browserProvider;
    private readonly IHubContext<BrowserStreamHub> _hubContext;
    private readonly ILogger<BrowserStreamingService> _logger;
    private readonly ConcurrentDictionary<string, BrowserStreamingSession> _sessions = new();
    private readonly Timer _cleanupTimer;

    public BrowserStreamingService(
        IBrowserInstanceProvider browserProvider,
        IHubContext<BrowserStreamHub> hubContext,
        ILogger<BrowserStreamingService> logger)
    {
        _browserProvider = browserProvider;
        _hubContext = hubContext;
        _logger = logger;
        
        // Cleanup inactive sessions every 5 minutes
        _cleanupTimer = new Timer(CleanupInactiveSessions, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task<string> CreateStreamingSessionAsync(string? initialUrl = null)
    {
        var sessionId = Guid.NewGuid().ToString("N")[..8]; // Short session ID
        
        try
        {
            var browserInstance = _browserProvider.GetBrowserInstance();

            var session = new BrowserStreamingSession
            {
                SessionId = sessionId,
                BrowserInstance = browserInstance,
                CreatedAt = DateTimeOffset.UtcNow,
                LastActivity = DateTimeOffset.UtcNow,
                IsStreaming = false
            };

            // Set up IBrowserInstance event handlers
            browserInstance.NavigationCompleted += async (url, title) => await OnPageLoad(sessionId);
            browserInstance.UrlChanged += async (url) => await OnUrlChanged(sessionId);
            browserInstance.TitleChanged += async (title) => await OnTitleChanged(sessionId);

            _sessions[sessionId] = session;

            if (!string.IsNullOrEmpty(initialUrl))
            {
                await NavigateAsync(sessionId, initialUrl);
            }

            _logger.LogInformation("Created streaming session {SessionId}", sessionId);
            return sessionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create streaming session");
            throw;
        }
    }

    public async Task CloseStreamingSessionAsync(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            try
            {
                await StopStreamingAsync(sessionId);
                
                // Remove event handlers to prevent memory leaks
                var browserInstance = session.BrowserInstance;
                browserInstance.NavigationCompleted -= async (url, title) => await OnPageLoad(sessionId);
                browserInstance.UrlChanged -= async (url) => await OnUrlChanged(sessionId);
                browserInstance.TitleChanged -= async (title) => await OnTitleChanged(sessionId);
                
                _logger.LogInformation("Closed streaming session {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing streaming session {SessionId}", sessionId);
            }
        }
    }

    public async Task NavigateAsync(string sessionId, string url)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        try
        {
            session.LastActivity = DateTimeOffset.UtcNow;
            
            // Ensure URL has protocol
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
            }

            await session.BrowserInstance.NavigateAsync(url);
            await session.BrowserInstance.WaitForLoadAsync();

            // Send page update to clients
            await BroadcastPageUpdate(sessionId, session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation failed for session {SessionId} to {Url}", sessionId, url);
            await _hubContext.Clients.Group($"browser-{sessionId}")
                .SendAsync("BrowserError", $"Navigation failed: {ex.Message}");
        }
    }

    public async Task HandleInteractionAsync(string sessionId, BrowserInteraction interaction)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return;
        }

        try
        {
            session.LastActivity = DateTimeOffset.UtcNow;
            var browser = session.BrowserInstance;

            switch (interaction.Type.ToLowerInvariant())
            {
                case "click":
                    if (interaction.X.HasValue && interaction.Y.HasValue)
                    {
                        await browser.ClickAsync(interaction.X.Value, interaction.Y.Value);
                    }
                    else if (!string.IsNullOrEmpty(interaction.Selector))
                    {
                        await browser.ClickElementAsync(interaction.Selector);
                    }
                    break;

                case "type":
                    if (!string.IsNullOrEmpty(interaction.Text))
                    {
                        if (!string.IsNullOrEmpty(interaction.Selector))
                        {
                            await browser.FillElementAsync(interaction.Selector, interaction.Text);
                        }
                        else
                        {
                            await browser.TypeAsync(interaction.Text);
                        }
                    }
                    break;

                case "keypress":
                    if (!string.IsNullOrEmpty(interaction.Key))
                    {
                        // Handle modifier keys by pressing them down first
                        if (interaction.CtrlKey)
                            await browser.KeyDownAsync("Control");
                        if (interaction.ShiftKey)
                            await browser.KeyDownAsync("Shift");
                        if (interaction.AltKey)
                            await browser.KeyDownAsync("Alt");

                        // Press the main key
                        await browser.KeyPressAsync(interaction.Key);

                        // Release modifier keys
                        if (interaction.AltKey)
                            await browser.KeyUpAsync("Alt");
                        if (interaction.ShiftKey)
                            await browser.KeyUpAsync("Shift");
                        if (interaction.CtrlKey)
                            await browser.KeyUpAsync("Control");
                    }
                    break;

                case "scroll":
                    if (interaction.X.HasValue && interaction.Y.HasValue)
                    {
                        await browser.ScrollAsync((int)interaction.X.Value, (int)interaction.Y.Value);
                    }
                    break;
            }

            // Capture and broadcast frame after interaction
            if (session.IsStreaming)
            {
                await CaptureAndBroadcastFrame(sessionId, session);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Interaction failed for session {SessionId}: {Interaction}", 
                sessionId, JsonSerializer.Serialize(interaction));
        }
    }

    public Task StartStreamingAsync(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return Task.CompletedTask;
        }

        if (session.IsStreaming)
        {
            return Task.CompletedTask; // Already streaming
        }

        session.IsStreaming = true;
        session.LastActivity = DateTimeOffset.UtcNow;

        // Start the streaming loop
        _ = Task.Run(async () => await StreamingLoop(sessionId));

        _logger.LogInformation("Started streaming for session {SessionId}", sessionId);
        return Task.CompletedTask;
    }

    public async Task StopStreamingAsync(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.IsStreaming = false;
            _logger.LogInformation("Stopped streaming for session {SessionId}", sessionId);
        }
        
        await Task.CompletedTask;
    }

    public async Task<BrowserSessionInfo?> GetSessionInfoAsync(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return null;
        }

        try
        {
            var browser = session.BrowserInstance;
            var title = await browser.GetTitleAsync();
            var url = browser.CurrentUrl;
            
            return new BrowserSessionInfo
            {
                SessionId = sessionId,
                Title = title,
                Url = url,
                CreatedAt = session.CreatedAt,
                LastActivity = session.LastActivity,
                IsStreaming = session.IsStreaming
            };
        }
        catch
        {
            return new BrowserSessionInfo
            {
                SessionId = sessionId,
                Title = "Error",
                Url = "about:blank",
                CreatedAt = session.CreatedAt,
                LastActivity = session.LastActivity,
                IsStreaming = session.IsStreaming
            };
        }
    }

    private async Task StreamingLoop(string sessionId)
    {
        const int targetFps = 10; // 10 FPS for reasonable performance
        var frameInterval = TimeSpan.FromMilliseconds(1000.0 / targetFps);

        while (_sessions.TryGetValue(sessionId, out var session) && session.IsStreaming)
        {
            try
            {
                await CaptureAndBroadcastFrame(sessionId, session);
                await Task.Delay(frameInterval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in streaming loop for session {SessionId}", sessionId);
                await Task.Delay(1000); // Wait before retrying
            }
        }
    }

    private async Task CaptureAndBroadcastFrame(string sessionId, BrowserStreamingSession session)
    {
        try
        {
            var screenshot = await session.BrowserInstance.TakeScreenshotAsync();

            var base64Frame = Convert.ToBase64String(screenshot);
            var frameData = $"data:image/jpeg;base64,{base64Frame}";

            await _hubContext.Clients.Group($"browser-{sessionId}")
                .SendAsync("FrameUpdate", frameData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture frame for session {SessionId}", sessionId);
        }
    }

    private async Task BroadcastPageUpdate(string sessionId, BrowserStreamingSession session)
    {
        try
        {
            var browser = session.BrowserInstance;
            var title = await browser.GetTitleAsync();
            var url = browser.CurrentUrl;

            await _hubContext.Clients.Group($"browser-{sessionId}")
                .SendAsync("PageUpdate", new
                {
                    Title = title,
                    Url = url,
                    Timestamp = DateTimeOffset.UtcNow
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast page update for session {SessionId}", sessionId);
        }
    }

    private async Task OnPageLoad(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            await BroadcastPageUpdate(sessionId, session);
        }
    }

    private async Task OnUrlChanged(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            await BroadcastPageUpdate(sessionId, session);
        }
    }

    private async Task OnTitleChanged(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            await BroadcastPageUpdate(sessionId, session);
        }
    }

    private void CleanupInactiveSessions(object? state)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-30); // Clean up sessions inactive for 30 minutes
        var inactiveSessions = _sessions
            .Where(kvp => kvp.Value.LastActivity < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var sessionId in inactiveSessions)
        {
            _ = Task.Run(async () => await CloseStreamingSessionAsync(sessionId));
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        
        var sessionIds = _sessions.Keys.ToList();
        foreach (var sessionId in sessionIds)
        {
            _ = Task.Run(async () => await CloseStreamingSessionAsync(sessionId));
        }
    }
}

// Supporting classes
public class BrowserStreamingSession
{
    public required string SessionId { get; init; }
    public required IBrowserInstance BrowserInstance { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastActivity { get; set; }
    public bool IsStreaming { get; set; }
}

public class BrowserInteraction
{
    public required string Type { get; init; } // click, type, keypress, scroll
    public string? Selector { get; init; }
    public double? X { get; init; }
    public double? Y { get; init; }
    public string? Text { get; init; }
    public string? Key { get; init; }
    public bool CtrlKey { get; init; }
    public bool ShiftKey { get; init; }
    public bool AltKey { get; init; }
}

public class BrowserSessionInfo
{
    public required string SessionId { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset LastActivity { get; init; }
    public required bool IsStreaming { get; init; }
}