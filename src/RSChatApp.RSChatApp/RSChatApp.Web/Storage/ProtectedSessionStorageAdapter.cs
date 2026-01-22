using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace RSChatApp.Web.Storage;

/// <summary>
/// Adapter for Blazor's ProtectedSessionStorage
/// Stores encrypted data in browser SessionStorage (cleared when tab/browser closes)
/// </summary>
public class ProtectedSessionStorageAdapter : IProtectedBrowserStorage
{
    private readonly ProtectedSessionStorage _storage;

    public ProtectedSessionStorageAdapter(ProtectedSessionStorage storage)
    {
        _storage = storage;
    }

    public async Task SetAsync<T>(string key, T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        await _storage.SetAsync(key, value);
    }

    public async Task<StorageResult<T>> GetAsync<T>(string key)
    {
        return (await _storage.GetAsync<T>(key))
            .ToStorageResult();
    }

    public async Task DeleteAsync(string key)
    {
        await _storage.DeleteAsync(key);
    }
}
