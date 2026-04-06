using System.Collections.Concurrent;

namespace RSChatApp.Application.Core.Chat;

public sealed class ActiveRequestRegistry : IActiveRequestRegistry
{
    private readonly ConcurrentDictionary<Guid, ActiveRequestHandle> _handles = new();

    public IPausableStreamControl Register(Guid requestId)
    {
        var handle = new ActiveRequestHandle();
        _handles[requestId] = handle;
        return handle;
    }

    public void Cancel(Guid requestId)
    {
        if (_handles.TryGetValue(requestId, out var handle))
            handle.Cancel();
    }

    public void Pause(Guid requestId)
    {
        if (_handles.TryGetValue(requestId, out var handle))
            handle.Pause();
    }

    public void Resume(Guid requestId)
    {
        if (_handles.TryGetValue(requestId, out var handle))
            handle.Resume();
    }

    public void Unregister(Guid requestId)
    {
        if (_handles.TryRemove(requestId, out var handle))
            handle.Dispose();
    }

    public IReadOnlyCollection<Guid> GetActiveRequestIds()
        => _handles.Keys.ToList().AsReadOnly();

    private sealed class ActiveRequestHandle : IPausableStreamControl, IDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly ManualResetEventSlim _gate = new(initialState: true);

        public CancellationToken Token => _cts.Token;
        public bool IsPaused => !_gate.IsSet;

        public void Pause() => _gate.Reset();
        public void Resume() => _gate.Set();

        public void Cancel()
        {
            _gate.Set(); // unblock if paused so cancellation propagates
            _cts.Cancel();
        }

        public void Dispose()
        {
            _gate.Set();
            _cts.Dispose();
            _gate.Dispose();
        }
    }
}

