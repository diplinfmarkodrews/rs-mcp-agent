using RSChatApp.Web.Models.Terminal;

namespace RSChatApp.Web.Storage.Terminal;

public class TerminalInstanceStorage : AbstractStorage<List<TerminalInstance>>
{
    public TerminalInstanceStorage(IProtectedBrowserStorage browserStorage) : base("terminals", browserStorage)
    {
    }
}