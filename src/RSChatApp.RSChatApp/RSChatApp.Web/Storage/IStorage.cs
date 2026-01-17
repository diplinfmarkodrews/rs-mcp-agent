using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Cryptography;

namespace RSChatApp.Web.Storage;

// Toplevel interface to use and implement
public interface IStorage<T>
{
    Task SaveAsync(T item);
    Task DeleteAsync();
    Task<ProtectedBrowserStorageResult<T>> GetAsync();
    
}

public abstract class AbstractStorage<T> : IStorage<T>
{
    protected readonly string StorageKey;
    private readonly IProtectedBrowserStorage _browserStorage;

    public AbstractStorage(string storageKey, IProtectedBrowserStorage browserStorage)
    {
        StorageKey = string.IsNullOrEmpty(storageKey) ? throw new ArgumentNullException(nameof(storageKey)) : storageKey;
        _browserStorage = browserStorage;
    }
    public virtual Task SaveAsync(T item)
    {
        return _browserStorage.SetAsync(StorageKey, item);
    }

    public virtual Task DeleteAsync()
    {
        return _browserStorage.DeleteAsync(StorageKey);
    }

    public virtual async Task<ProtectedBrowserStorageResult<T>> GetAsync()
    {
        try
        {
            return await _browserStorage.GetAsync<T>(StorageKey);
        }
        catch (CryptographicException)
        {
            // Typically happens when data protection keys changed and old browser payloads can no longer be decrypted.
            // Treat it as cache-miss and clear the corrupted value.
            await _browserStorage.DeleteAsync(StorageKey);
            return new ProtectedBrowserStorageResult<T>();
        }
    }
} 