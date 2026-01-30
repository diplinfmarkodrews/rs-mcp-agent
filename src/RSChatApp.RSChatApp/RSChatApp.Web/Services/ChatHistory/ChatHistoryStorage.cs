using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Polly;
using RSChatApp.Mcp.ExtensionAI.Processing;
using RSChatApp.Web.Storage;

namespace RSChatApp.Web.Services.ChatHistory;

/// <summary>
/// Storage for chat history with custom JSON serialization to preserve AIContent polymorphism.
/// ProtectedBrowserStorage doesn't use global JsonSerializerOptions, so we serialize to string manually.
/// </summary>
public class ChatHistoryStorage : AbstractStorage<List<ChatMessage>> 
{
    private readonly ILogger _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new ChatMessageConverter() }
    };
    
    public ChatHistoryStorage(ILogger<ChatHistoryStorage> logger, IProtectedBrowserStorage browserStorage) : base("chatHistory", browserStorage)
    {
        _logger = logger;
    }
    
    public override async Task SaveAsync(List<ChatMessage> item)
    {
        try
        {
            // Serialize to JSON string with our custom converter
            var json = JsonSerializer.Serialize(item, JsonOptions);
            
            _logger.LogInformation("Saving {messageCount} messages to storage key '{storageKey}', JSON length: {jsonLength}", item.Count, StorageKey, json.Length);
            
            // Store the JSON string (ProtectedBrowserStorage will encrypt it)
            await BrowserStorage.SetAsync(StorageKey, json);
            
            _logger.LogInformation("Successfully saved chat history to storage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save chat history: {message}", ex.Message);
            throw;
        }
    }
    
    public override async Task<StorageResult<List<ChatMessage>>> GetAsync()
    {
        return await RetryPolicy()
            .ExecuteAsync(async () =>
            {
                try
                {
                    _logger.LogInformation("Attempting to load chat history from storage key '{storageKey}'", StorageKey);

                    // Get the JSON string
                    var result = await BrowserStorage.GetAsync<string>(StorageKey);

                    _logger.LogInformation("Storage result - Success: {success}, HasValue: {hasValue}, ValueLength: {length}",
                        result.Success,
                        result.Value != null,
                        result.Value?.Length ?? 0);

                    if (!result.Success || string.IsNullOrEmpty(result.Value))
                    {
                        _logger.LogInformation("No chat history found in storage (Success={success}, IsNullOrEmpty={isEmpty})",
                            result.Success,
                            string.IsNullOrEmpty(result.Value));
                        return new StorageResult<List<ChatMessage>>();
                    }

                    _logger.LogInformation("Retrieved JSON from storage, attempting deserialization. Length: {length}", result.Value.Length);

                    // Deserialize with our custom converter
                    var messages = JsonSerializer.Deserialize<List<ChatMessage>>(result.Value, JsonOptions);

                    _logger.LogInformation("Successfully deserialized {messageCount} messages from storage", messages?.Count ?? 0);
                    foreach (var chatMessage in messages)
                        _logger.LogDebug("Successfully deserialized message from storage: {Messages}", JsonSerializer.Serialize(chatMessage, JsonOptions));

                    return new StorageResult<List<ChatMessage>>
                    {
                        Success = true,
                        Value = messages ?? new List<ChatMessage>()
                    };
                }
                catch (CryptographicException cryptoEx)
                {
                    // Data protection keys changed, clear corrupted value
                    _logger.LogWarning(cryptoEx, "CryptographicException loading chat history, clearing storage");
                    await BrowserStorage.DeleteAsync(StorageKey);
                    return new StorageResult<List<ChatMessage>>();
                }
                catch (JsonException jsonException)
                {
                    // Corrupted JSON, clear it
                    _logger.LogError(jsonException, "JsonException loading chat history: {Message}", jsonException.Message);
                    await BrowserStorage.DeleteAsync(StorageKey);
                    return new StorageResult<List<ChatMessage>>();
                }
                catch (TaskCanceledException canceledException)
                {
                    // JS interop not ready yet (common during prerender). Throw so Polly can retry immediately.
                    _logger.LogDebug("Chat history loading cancelled (JS interop not ready, likely during prerender): {Message}", canceledException.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error loading chat history: {Message}", ex.Message);
                    return new StorageResult<List<ChatMessage>>();
                }
            });
    }

    private AsyncPolicy<StorageResult<List<ChatMessage>>> RetryPolicy()
    {
        var retry = Policy<StorageResult<List<ChatMessage>>>
            .Handle<TaskCanceledException>()
            .RetryAsync(3, onRetry: (_, retryNumber) =>
            {
                _logger.LogWarning("Retry {RetryNumber} loading chat history due to task cancellation", retryNumber);
            });

        var fallback = Policy<StorageResult<List<ChatMessage>>>
            .Handle<TaskCanceledException>()
            .FallbackAsync((_) =>
            {
                _logger.LogWarning("Chat history load cancelled after retries; returning empty history");
                return Task.FromResult(new StorageResult<List<ChatMessage>>());
            });

        return fallback.WrapAsync(retry);
    }
}
