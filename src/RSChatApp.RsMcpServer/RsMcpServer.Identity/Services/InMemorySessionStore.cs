using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
}

/// <summary>
/// <see cref="IMemoryCache"/> based session store; entries decay at the session's <c>ExpiresAt</c>,
/// so no cleanup job is needed.
/// </summary>
public class InMemorySessionStore : ISessionStore
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemorySessionStore> _logger;

    public InMemorySessionStore(IMemoryCache cache, ILogger<InMemorySessionStore> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    private static string SessionKey(string token) => $"session:{token}";
    private static string UserKey(string username) => $"user-session:{username}";

    public Task StoreSessionAsync(string token, TokenAuthenticatedSession session, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
            throw new ArgumentException("Token cannot be null or empty", nameof(token));

        if (session?.User?.Identity?.Name == null)
            throw new ArgumentException("Session must have a valid user", nameof(session));

        var username = session.User.Identity.Name;

        // Single session per user: drop the previous one unless it is the same token being refreshed
        if (_cache.TryGetValue(UserKey(username), out string? previousToken) && previousToken != token)
            _cache.Remove(SessionKey(previousToken!));

        var options = new MemoryCacheEntryOptions { AbsoluteExpiration = new DateTimeOffset(session.ExpiresAt.ToUniversalTime()) };
        _cache.Set(SessionKey(token), session, options);
        _cache.Set(UserKey(username), token, options);

        _logger.LogDebug("Stored session for user {Username} with token {TokenPrefix}***",
            username, token[..Math.Min(8, token.Length)]);

        return Task.CompletedTask;
    }

    public Task<TokenAuthenticatedSession?> GetSessionAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
            return Task.FromResult<TokenAuthenticatedSession?>(null);

        return Task.FromResult(_cache.Get<TokenAuthenticatedSession>(SessionKey(token)));
    }

    public Task RemoveSessionAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
            return Task.CompletedTask;

        if (_cache.TryGetValue(SessionKey(token), out TokenAuthenticatedSession? session))
        {
            _cache.Remove(SessionKey(token));
            var username = session?.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(username) && _cache.TryGetValue(UserKey(username), out string? userToken) && userToken == token)
                _cache.Remove(UserKey(username));

            _logger.LogDebug("Removed session for token {TokenPrefix}***",
                token[..Math.Min(8, token.Length)]);
        }

        return Task.CompletedTask;
    }

    public Task RemoveUserSessionsAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(username))
            return Task.CompletedTask;

        if (_cache.TryGetValue(UserKey(username), out string? token))
        {
            _cache.Remove(SessionKey(token!));
            _cache.Remove(UserKey(username));
            _logger.LogDebug("Removed all sessions for user {Username}", username);
        }

        return Task.CompletedTask;
    }
}
