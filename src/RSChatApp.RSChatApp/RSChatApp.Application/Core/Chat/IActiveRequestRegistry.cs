namespace RSChatApp.Application.Core.Chat;

public interface IActiveRequestRegistry
{
    IPausableStreamControl Register(Guid requestId);
    void Cancel(Guid requestId);
    void Pause(Guid requestId);
    void Resume(Guid requestId);
    void Unregister(Guid requestId);
    IReadOnlyCollection<Guid> GetActiveRequestIds();
}

