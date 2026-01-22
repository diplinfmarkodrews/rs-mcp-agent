using System.Security.Cryptography;
using Microsoft.Extensions.AI;
using RSChatApp.Web.Storage;
using System.Text.Json;
using System.Text.Json.Serialization;


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
            
            _logger.LogDebug("Serialized {messageCount} messages, JSON length: {jsonLength}", item.Count, json.Length);
            
            // Store the JSON string (ProtectedBrowserStorage will encrypt it)
            await BrowserStorage.SetAsync(StorageKey, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save chat history: {message}", ex.Message);
            throw;
        }
    }
    
    public override async Task<StorageResult<List<ChatMessage>>> GetAsync()
    {
        try
        {
            // Get the JSON string
            var result = await BrowserStorage.GetAsync<string>(StorageKey);
            
            if (!result.Success || string.IsNullOrEmpty(result.Value))
            {
                _logger.LogDebug("No chat history found in storage");
                return new StorageResult<List<ChatMessage>>();
            }
            
            _logger.LogDebug("Retrieved JSON from storage, length: {length}", result.Value.Length);
            
            // Deserialize with our custom converter
            var messages = JsonSerializer.Deserialize<List<ChatMessage>>(result.Value, JsonOptions);
            
            _logger.LogInformation("Successfully deserialized {messageCount} messages from storage", messages?.Count ?? 0);
            
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
        catch (TaskCanceledException)
        {
            // JS interop not ready yet, return empty result
            _logger.LogDebug("Chat history loading cancelled (JS interop not ready)");
            return new StorageResult<List<ChatMessage>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading chat history: {Message}", ex.Message);
            return new StorageResult<List<ChatMessage>>();
        }
    }
}
