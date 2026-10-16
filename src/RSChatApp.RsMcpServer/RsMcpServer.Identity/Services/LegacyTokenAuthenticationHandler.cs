using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RsMcpServer.Identity.Services;

public static class AuthSchemes
{
    public const string MultiAuth = "MultiAuth";
    public const string Legacy = "LegacyToken";
    public const string Cookie = "Cookies";
}

/// <summary>
/// Authenticates "Authorization: Bearer &lt;GUID&gt;" tokens issued by /api/auth/v1/login against the session store.
/// </summary>
public class LegacyTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ISessionStore _sessionStore;

    public LegacyTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISessionStore sessionStore) : base(options, logger, encoder)
    {
        _sessionStore = sessionStore;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = BearerToken.FromHeader(Request.Headers.Authorization.ToString());
        if (token == null || !Guid.TryParse(token, out _))
            return AuthenticateResult.NoResult();

        var session = await _sessionStore.GetSessionAsync(token, Context.RequestAborted);
        if (session == null)
            return AuthenticateResult.Fail("Invalid or expired token");

        return AuthenticateResult.Success(
            new AuthenticationTicket(session.User, Scheme.Name));
    }
}

public static class BearerToken
{
    public static string? FromHeader(string? authorization)
    {
        if (string.IsNullOrWhiteSpace(authorization) ||
            !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;
        var token = authorization[7..].Trim();
        return token.Length == 0 ? null : token;
    }

    /// <summary>Keycloak access tokens are JWTs (three dot-separated segments); legacy tokens are GUIDs.</summary>
    public static bool IsJwt(string token) => token.Count(c => c == '.') == 2;
}
