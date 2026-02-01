using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Cryptography;

namespace RSChatApp.Web.Storage;

// Toplevel interface to use and implement
public interface IStorage<T>
{
    Task SaveAsync(T item);
    Task DeleteAsync();
    Task<StorageResult<T>> GetAsync();
    
}

public abstract class AbstractStorage<T> : IStorage<T>
{
    protected readonly string StorageKey;
    protected readonly IProtectedBrowserStorage BrowserStorage;

    public AbstractStorage(string storageKey, IProtectedBrowserStorage browserStorage)
    {
        StorageKey = string.IsNullOrEmpty(storageKey) ? throw new ArgumentNullException(nameof(storageKey)) : storageKey;
        BrowserStorage = browserStorage;
    }
    public virtual Task SaveAsync(T item)
    {
        return BrowserStorage.SetAsync(StorageKey, item);
    }

    public virtual Task DeleteAsync()
    {
        return BrowserStorage.DeleteAsync(StorageKey);
    }

    public virtual async Task<StorageResult<T>> GetAsync()
    {
        try
        {
            return await BrowserStorage.GetAsync<T>(StorageKey);
        }
        catch (CryptographicException)
        {
            // Typically happens when data protection keys changed and old browser payloads can no longer be decrypted.
            // Treat it as cache-miss and clear the corrupted value.
            await BrowserStorage.DeleteAsync(StorageKey);
            return new StorageResult<T>();
        }
    }
} 