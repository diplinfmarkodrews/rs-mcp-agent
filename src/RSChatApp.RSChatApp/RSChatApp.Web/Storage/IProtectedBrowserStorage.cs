using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace RSChatApp.Web.Storage;

/// <summary>
/// Abstraction over Blazor's protected browser storage (LocalStorage or SessionStorage)
/// Provides encrypted storage in the browser with a simplified async interface
/// </summary>
public interface IProtectedBrowserStorage
{
    /// <summary>
    /// Stores a value in encrypted browser storage
    /// </summary>
    Task SetAsync<T>(string key, T value);
    
    /// <summary>
    /// Attempts to retrieve a value from encrypted browser storage
    /// </summary>
    /// <returns>StorageResult indicating success and the value if found</returns>
    Task<StorageResult<T>> GetAsync<T>(string key);
    
    /// <summary>
    /// Removes a value from encrypted browser storage
    /// </summary>
    Task DeleteAsync(string key);
}
public record StorageResult<T>
{
    public T? Value { get; init; }
    public bool Success { get; init; } = false;
}

public static class StorageResultExtensions
{
    public static StorageResult<T> ToStorageResult<T>(this ProtectedBrowserStorageResult<T> value)
    {
        return new StorageResult<T>
        {
            Success = value.Success,
            Value = value.Value
        };
    }
}
