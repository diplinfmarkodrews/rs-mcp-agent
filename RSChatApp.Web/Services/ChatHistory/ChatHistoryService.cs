using Microsoft.Extensions.AI;
using RSChatApp.Web.Storage;

namespace RSChatApp.Web.Services.ChatHistory;

public interface IChatHistoryService
{
    Task SaveHistoryAsync(List<ChatMessage> messages);
    Task<List<ChatMessage>> LoadHistoryAsync();
    Task ClearHistoryAsync();
}



/// <summary>
/// This is a temporary storage for messagehistory in ProtectedLocalStorage
/// data is encrypted and stored in local browsercache
/// </summary>
public class ChatHistoryService : IChatHistoryService
{
    private readonly IProtectedBrowserStorage _storage;
    
    public ChatHistoryService(IProtectedBrowserStorage storage)
    {
        _storage = storage;
    }
    public async Task SaveHistoryAsync(List<ChatMessage> messages) 
    {
        await _storage.SetAsync("chatHistory", ValidateMessages(messages));
    }
    
    public async Task<List<ChatMessage>> LoadHistoryAsync() 
    {
        var result = await _storage.GetAsync<List<ChatMessage>>("chatHistory");
        return result.Success
            ? result.Value!
            : new();
    }
    public async Task ClearHistoryAsync() 
    {
        await _storage.DeleteAsync("chatHistory");
    }
    private static List<ChatMessage> ValidateMessages(List<ChatMessage> messages)
    {
        // Filters empty user messages
        return messages.Where(m => (string.IsNullOrEmpty(m.Text) 
            && m.Role == ChatRole.User) == false).ToList();
        
    }
}