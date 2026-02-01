using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace RSChatApp.Web.Storage;

/// <summary>
/// Adapter for Blazor's ProtectedLocalStorage
/// Stores encrypted data in browser LocalStorage (persists across browser sessions)
/// </summary>
public class ProtectedLocalStorageAdapter : IProtectedBrowserStorage
{
    private readonly ProtectedLocalStorage _storage;

    public ProtectedLocalStorageAdapter(ProtectedLocalStorage storage)
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
