using Microsoft.Extensions.AI;
using RSChatApp.Web.Storage;

namespace RSChatApp.Web.Services.ChatHistory;

public class ChatHistoryStorage : AbstractStorage<List<ChatMessage>> 
{
    public ChatHistoryStorage(IProtectedBrowserStorage browserStorage) : base("chatHistory", browserStorage)
    {
    }
}