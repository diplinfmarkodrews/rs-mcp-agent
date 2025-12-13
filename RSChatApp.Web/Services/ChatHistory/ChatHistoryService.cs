using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.AI;

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
    private readonly ProtectedLocalStorage _localStorage;
    
    public ChatHistoryService(ProtectedLocalStorage localStorage)
    {
        _localStorage = localStorage;
    }
    public async Task SaveHistoryAsync(List<ChatMessage> messages) 
    {
        await _localStorage.SetAsync("chatHistory", messages);
    }
    
    public async Task<List<ChatMessage>> LoadHistoryAsync() 
    {
        var result = await _localStorage.GetAsync<List<ChatMessage>>("chatHistory");
        return result.Success 
            ? result.Value 
            : new();
    }
    public async Task ClearHistoryAsync() 
    {
        await _localStorage.DeleteAsync("chatHistory");
    }
}