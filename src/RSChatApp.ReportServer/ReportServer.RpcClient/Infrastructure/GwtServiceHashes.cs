namespace ReportServer.RpcClient.Infrastructure;

public enum GwtService
{
    Login,
    Security,
    Challenge,
    Terminal
}

/// <summary>
/// Resolves GWT RPC service strong-name hashes for a configured ReportServer version.
/// <see cref="Baseline"/> holds the hashes of the current version (6.1 and newer); older versions only list what differs.
/// A key is either "Service" (whole service) or "Service.method" (single method override).
/// </summary>
public sealed class GwtServiceHashes
{
    public const string DefaultVersion = "6.1";

    private static readonly Dictionary<string, string> Baseline = new()
    {
        [nameof(GwtService.Login)] = "DFEDD0FBBBBBE222F217D04F50A95F56",
        [nameof(GwtService.Security)] = "1D8BB90B3362E3AB16AD5D9EC9568CE7",
        [nameof(GwtService.Challenge)] = "B6F10AD9852902823F606D81A985ACC7",
        [nameof(GwtService.Terminal)] = "3F77E49DD28B2F8553E17EF03CCDE319",
    };

    // Overrides apply to all versions strictly below the given version; the first matching entry wins.
    private static readonly (Version Below, Dictionary<string, string> Hashes)[] LegacyOverrides =
    {
        (new Version(6, 1), new()
        {
            [nameof(GwtService.Terminal)] = "BF140EBA9A84651D0CC50CCD75BC2C4F",
        }),
    };

    private readonly Dictionary<string, string> _overrides;

    public GwtServiceHashes(string? version)
    {
        var raw = string.IsNullOrWhiteSpace(version) ? DefaultVersion : version.Trim();
        // A missing minor counts as 0 ("6" -> "6.0")
        if (!raw.Contains('.'))
            raw += ".0";
        if (!Version.TryParse(raw, out var parsed))
            throw new InvalidOperationException($"Invalid ReportServer version '{version}'. Expected e.g. '6.1' or '6.1.4'.");

        Version = parsed;
        _overrides = LegacyOverrides.FirstOrDefault(l => parsed < l.Below).Hashes ?? new();
    }

    public Version Version { get; }

    public string Get(GwtService service, string? method = null)
    {
        var key = service.ToString();
        if (method is not null && _overrides.TryGetValue($"{key}.{method}", out var methodHash))
            return methodHash;
        return _overrides.TryGetValue(key, out var hash) ? hash : Baseline[key];
    }
}
