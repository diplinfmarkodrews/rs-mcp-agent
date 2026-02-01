using RSChatApp.Web.Models.Terminal;

namespace RSChatApp.Web.Services.Terminal.Drivers;

/// <summary>
/// Factory for creating terminal drivers based on terminal type
/// </summary>
public class TerminalDriverFactory
{
    private readonly IServiceProvider _serviceProvider;

    public TerminalDriverFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets the appropriate terminal driver for the specified type
    /// </summary>
    public ITerminalDriver GetDriver(TerminalType type)
    {
        return type switch
        {
            TerminalType.ReportServer => _serviceProvider.GetRequiredService<RsTerminalDriver>(),
            TerminalType.JavaScript => _serviceProvider.GetRequiredService<JsTerminalDriver>(),
            _ => throw new ArgumentException($"Unknown terminal type: {type}", nameof(type))
        };
    }
}
