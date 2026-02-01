using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RsMcpServer.Identity.Models.Authentication;

namespace RsMcpServer.Identity.Services;

/// <summary>
/// Service for managing authentication sessions in memory
/// </summary>
public interface ISessionStore
{
    /// <summary>
    /// Store an authentication session
    /// </summary>
    Task StoreSessionAsync(string token, TokenAuthenticatedSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve an authentication session
    /// </summary>
    Task<TokenAuthenticatedSession?> GetSessionAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove an authentication session
    /// </summary>
    Task RemoveSessionAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove all sessions for a specific user
    /// </summary>
    Task RemoveUserSessionsAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up expired sessions
    /// </summary>
    Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of session store
/// </summary>
public class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, TokenAuthenticatedSession> _sessions = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _userSessions = new();
    private readonly ILogger<InMemorySessionStore> _logger;
    private readonly Timer _cleanupTimer;

    public InMemorySessionStore(ILogger<InMemorySessionStore> logger)
    {
        _logger = logger;
        
        // Run cleanup every 5 minutes
        _cleanupTimer = new Timer(
            async _ => await CleanupExpiredSessionsAsync(), 
            null, 
            TimeSpan.FromMinutes(5), 
            TimeSpan.FromMinutes(5));
    }

    public Task StoreSessionAsync(string token, TokenAuthenticatedSession session, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
            throw new ArgumentException("Token cannot be null or empty", nameof(token));
        
        if (session?.User?.Identity?.Name == null)
            throw new ArgumentException("Session must have a valid user", nameof(session));

        var username = session.User.Identity.Name;
        
        // Remove existing sessions for this user (single session per user)
        if (_userSessions.TryGetValue(username, out var existingSessions))
        {
            foreach (var existingToken in existingSessions.ToList())
            {
                _sessions.TryRemove(existingToken, out _);
            }
        }

        // Store new session
        _sessions[token] = session;
        // Track user sessions
        _userSessions.AddOrUpdate(username, 
            new HashSet<string> { token },
            (_, sessions) => 
            {
                sessions.Clear();
                sessions.Add(token);
                return sessions;
            });

        _logger.LogDebug("Stored session for user {Username} with token {TokenPrefix}***", 
            username, token[..Math.Min(8, token.Length)]);

        return Task.CompletedTask;
    }

    public Task<TokenAuthenticatedSession?> GetSessionAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
            return Task.FromResult<TokenAuthenticatedSession?>(null);

        if (!_sessions.TryGetValue(token, out var session))
            return Task.FromResult<TokenAuthenticatedSession?>(null);

        // Check if session is expired
        if (session.ExpiresAt <= DateTime.UtcNow)
        {
            // Remove expired session
            _ = Task.Run(() => RemoveSessionAsync(token, cancellationToken), cancellationToken);
            return Task.FromResult<TokenAuthenticatedSession?>(null);
        }

        return Task.FromResult<TokenAuthenticatedSession?>(session);
    }

    public Task RemoveSessionAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
            return Task.CompletedTask;

        if (_sessions.TryRemove(token, out var session))
        {
            var username = session.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(username) && _userSessions.TryGetValue(username, out var userTokens))
            {
                userTokens.Remove(token);
                if (userTokens.Count == 0)
                {
                    _userSessions.TryRemove(username, out _);
                }
            }

            _logger.LogDebug("Removed session for token {TokenPrefix}***", 
                token[..Math.Min(8, token.Length)]);
        }

        return Task.CompletedTask;
    }

    public Task RemoveUserSessionsAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(username))
            return Task.CompletedTask;

        if (_userSessions.TryRemove(username, out var userTokens))
        {
            foreach (var token in userTokens)
            {
                _sessions.TryRemove(token, out _);
            }

            _logger.LogDebug("Removed all sessions for user {Username}", username);
        }

        return Task.CompletedTask;
    }

    public Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiredTokens = _sessions
            .Where(kvp => kvp.Value.ExpiresAt <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var token in expiredTokens)
        {
            _ = RemoveSessionAsync(token, cancellationToken);
        }

        if (expiredTokens.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} expired sessions", expiredTokens.Count);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}
