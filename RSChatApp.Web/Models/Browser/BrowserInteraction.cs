namespace RSChatApp.Web.Models.Browser;

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