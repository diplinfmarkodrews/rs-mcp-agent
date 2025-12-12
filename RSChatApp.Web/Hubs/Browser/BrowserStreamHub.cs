using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace RSChatApp.Web.Hubs;

/// <summary>
/// SignalR Hub for real-time browser streaming and interaction
/// </summary>
public class BrowserStreamHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> _connectionToBrowserSession = new();
    private static readonly ConcurrentDictionary<string, HashSet<string>> _browserSessionToConnections = new();

    public async Task JoinBrowserSession(string sessionId)
    {
        // Remove from previous session if exists
        if (_connectionToBrowserSession.TryGetValue(Context.ConnectionId, out var previousSession))
        {
            await LeaveBrowserSession(previousSession);
        }

        // Add to new session
        _connectionToBrowserSession[Context.ConnectionId] = sessionId;
        
        if (!_browserSessionToConnections.ContainsKey(sessionId))
        {
            _browserSessionToConnections[sessionId] = new HashSet<string>();
        }
        
        _browserSessionToConnections[sessionId].Add(Context.ConnectionId);
        
        await Groups.AddToGroupAsync(Context.ConnectionId, $"browser-{sessionId}");
        
        // Notify other clients in the session
        await Clients.Group($"browser-{sessionId}")
            .SendAsync("UserJoined", Context.ConnectionId);
    }

    public async Task LeaveBrowserSession(string sessionId)
    {
        _connectionToBrowserSession.TryRemove(Context.ConnectionId, out _);
        
        if (_browserSessionToConnections.TryGetValue(sessionId, out var connections))
        {
            connections.Remove(Context.ConnectionId);
            if (connections.Count == 0)
            {
                _browserSessionToConnections.TryRemove(sessionId, out _);
            }
        }
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"browser-{sessionId}");
        
        // Notify other clients in the session
        await Clients.Group($"browser-{sessionId}")
            .SendAsync("UserLeft", Context.ConnectionId);
    }

    public async Task SendInteraction(string sessionId, string interactionType, object data)
    {
        // Forward interaction to the browser session handler
        await Clients.Group($"browser-{sessionId}")
            .SendAsync("BrowserInteraction", new
            {
                Type = interactionType,
                Data = data,
                ConnectionId = Context.ConnectionId,
                Timestamp = DateTimeOffset.UtcNow
            });
    }

    public async Task SendMouseMove(string sessionId, double x, double y)
    {
        await Clients.Group($"browser-{sessionId}")
            .SendAsync("MouseMove", new { X = x, Y = y, ConnectionId = Context.ConnectionId });
    }

    public async Task SendClick(string sessionId, double x, double y, string button = "left")
    {
        await Clients.Group($"browser-{sessionId}")
            .SendAsync("MouseClick", new { X = x, Y = y, Button = button, ConnectionId = Context.ConnectionId });
    }

    public async Task SendKeyPress(string sessionId, string key, bool ctrlKey = false, bool shiftKey = false, bool altKey = false)
    {
        await Clients.Group($"browser-{sessionId}")
            .SendAsync("KeyPress", new 
            { 
                Key = key, 
                CtrlKey = ctrlKey, 
                ShiftKey = shiftKey, 
                AltKey = altKey, 
                ConnectionId = Context.ConnectionId 
            });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectionToBrowserSession.TryGetValue(Context.ConnectionId, out var sessionId))
        {
            await LeaveBrowserSession(sessionId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    // Methods for the browser service to send updates to clients
    public async Task BroadcastFrame(string sessionId, string frameData)
    {
        await Clients.Group($"browser-{sessionId}")
            .SendAsync("FrameUpdate", frameData);
    }

    public async Task BroadcastPageUpdate(string sessionId, object pageInfo)
    {
        await Clients.Group($"browser-{sessionId}")
            .SendAsync("PageUpdate", pageInfo);
    }

    public async Task BroadcastError(string sessionId, string error)
    {
        await Clients.Group($"browser-{sessionId}")
            .SendAsync("BrowserError", error);
    }
}