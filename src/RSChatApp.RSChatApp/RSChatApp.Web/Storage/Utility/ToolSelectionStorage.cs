using RSChatApp.Web.Storage;

namespace RSChatApp.Web.Storage.Utility;

public class ToolSelectionStorage : AbstractStorage<HashSet<string>>
{
    private HashSet<string> _enabledTools = [];

    public ToolSelectionStorage(IProtectedBrowserStorage browserStorage)
        : base("tool-selection", browserStorage) { }

    public async Task InitializeAllAsync(IEnumerable<string> toolNames)
    {
        var result = await GetAsync();
        if (result.Success && result.Value is { Count: > 0 })
        {
            _enabledTools = result.Value;
        }
        else
        {
            _enabledTools = [..toolNames];
            await SaveAsync(_enabledTools);
        }
    }

    public bool IsEnabled(string toolName) => _enabledTools.Contains(toolName);

    public async Task SetEnabledAsync(string toolName, bool enabled)
    {
        if (enabled) _enabledTools.Add(toolName);
        else _enabledTools.Remove(toolName);
        await SaveAsync(_enabledTools);
    }
}