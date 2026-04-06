namespace RSChatApp.Application.Core.Chat;

public interface IPausableStreamControl
{
    CancellationToken Token { get; }
    bool IsPaused { get; }
    void Pause();
    void Resume();
    void Cancel();
}

