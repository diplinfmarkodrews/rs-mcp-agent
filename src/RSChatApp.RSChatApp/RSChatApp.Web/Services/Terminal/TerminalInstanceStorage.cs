using RSChatApp.Web.Models.Terminal;
using RSChatApp.Web.Storage;

namespace RSChatApp.Web.Services.Terminal;

public class TerminalInstanceStorage : AbstractStorage<List<TerminalInstance>>
{
    public TerminalInstanceStorage(IProtectedBrowserStorage browserStorage) : base("terminals", browserStorage)
    {
    }
}