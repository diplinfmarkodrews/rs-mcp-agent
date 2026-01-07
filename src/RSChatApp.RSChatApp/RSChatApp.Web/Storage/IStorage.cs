using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

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

    public virtual Task<ProtectedBrowserStorageResult<T>> GetAsync()
    {
        return _browserStorage.GetAsync<T>(StorageKey);
    }
} 