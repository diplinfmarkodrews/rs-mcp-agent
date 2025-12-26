using RSChatApp.Web.Models.Terminal;
using RSChatApp.Web.Services.Terminal.Drivers;
using RSChatApp.Web.Storage;

namespace RSChatApp.Web.Services.Terminal;

public interface ITerminalManager
{
    event Action? Changed;

    IReadOnlyList<TerminalInstance> Terminals { get; }
    Guid ActiveTerminalId { get; }
    TerminalInstance? ActiveTerminal { get; }

    Task<TerminalInstance> CreateAsync(TerminalType type, CancellationToken cancellationToken = default);
    Task CloseAsync(Guid terminalId, CancellationToken cancellationToken = default);

    Task SetActiveAsync(Guid terminalId, CancellationToken cancellationToken = default);
    Task SetMinimizedAsync(Guid terminalId, bool minimized, CancellationToken cancellationToken = default);

    Task<CommandEntry?> ExecuteAsync(Guid terminalId, string command, CancellationToken cancellationToken = default);

    string NavigateHistory(Guid terminalId, int direction, string currentInput);
    void ResetHistoryNavigation(Guid terminalId);

    Task PersistAsync(CancellationToken cancellationToken = default);
}

public sealed class TerminalManagerService : ITerminalManager
{
    private const int MaxSeededHistoryEntriesPerType = 200;
    
    private readonly IStorage<List<TerminalInstance>> _terminalStorage;
    private readonly TerminalDriverFactory _driverFactory;
    private readonly ILogger<TerminalManagerService> _logger;

    private readonly List<TerminalInstance> _terminals = new();
    private readonly Dictionary<TerminalType, List<CommandEntry>> _historyByType = new();

    // Per-terminal navigation state (not persisted)
    private readonly Dictionary<Guid, int> _historyIndexByTerminalId = new();
    private readonly Dictionary<Guid, string> _draftInputByTerminalId = new();

    // Per-terminal execution lock
    private readonly Dictionary<Guid, SemaphoreSlim> _terminalExecutionLocks = new();

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;
    private int _terminalCounter = 1;

    private Guid _activeTerminalId;

    public TerminalManagerService(
        IStorage<List<TerminalInstance>> terminalStorage,
        TerminalDriverFactory driverFactory,
        ILogger<TerminalManagerService> logger)
    {
        _terminalStorage = terminalStorage;
        _driverFactory = driverFactory;
        _logger = logger;
    }

    private Action? _changed;

    public event Action? Changed
    {
        add
        {
            _changed += value;
            _ = EnsureInitializedInBackgroundAsync();
        }
        remove => _changed -= value;
    }

    public IReadOnlyList<TerminalInstance> Terminals => _terminals;

    public Guid ActiveTerminalId => _activeTerminalId;

    public TerminalInstance? ActiveTerminal => _terminals.FirstOrDefault(t => t.Id == _activeTerminalId);

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
            return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
                return;

            var loaded = await _terminalStorage.GetAsync();
            
            _terminals.Clear();
            if (loaded.Success)
                _terminals.AddRange(loaded.Value!);

            _terminalCounter = _terminals.Count + 1;

            await ValidateLoadedSessionsAsync(cancellationToken);
            RebuildHistoryCacheFromInstances();

            if (_terminals.Any())
            {
                _activeTerminalId = _terminals.FirstOrDefault(t => t.IsValid)?.Id ?? _terminals.First().Id;
            }

            // Persist any session validation/prompt fixes.
            await _terminalStorage.SaveAsync(_terminals);

            _initialized = true;
            RaiseChanged();
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task EnsureInitializedInBackgroundAsync()
    {
        try
        {
            await EnsureInitializedAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "TerminalManager initialization failed");
        }
    }

    public async Task<TerminalInstance> CreateAsync(TerminalType type, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var seededHistory = GetSeedHistoryForType(type);

        var terminal = new TerminalInstance
        {
            Id = Guid.NewGuid(),
            Type = type,
            Name = $"{type}-{_terminalCounter++}",
            IsMinimized = false,
            IsValid = true,
            CommandHistory = seededHistory
        };

        _terminals.Add(terminal);
        _activeTerminalId = terminal.Id;

        ResetHistoryNavigation(terminal.Id);

        // Initialize the driver session immediately for new terminals.
        // If initialization fails, we keep the terminal but mark it invalid and surface the error.
        try
        {
            var driver = _driverFactory.GetDriver(type);
            var initResult = await driver.InitSessionAsync(cancellationToken);

            if (initResult.IsSuccess && initResult.Data != null)
            {
                terminal.SessionId = initResult.Data.SessionId;
                terminal.Prompt = initResult.Data.Prompt;
                terminal.WorkingDirectory = initResult.Data.WorkingDirectory;
                terminal.IsValid = true;
            }
            else
            {
                terminal.IsValid = false;
                string errorMessage = "Failed to initialize session." + (initResult.Message ?? string.Empty);
                var initErrorEntry = new CommandEntry
                {
                    Command = "(init)",
                    Output = errorMessage,
                    Error = errorMessage,
                    IsSuccess = false
                };

                terminal.CommandHistory.Add(initErrorEntry);
                CollectHistoryForType(type, initErrorEntry);
            }
        }
        catch (Exception ex)
        {
            terminal.IsValid = false;
            string errorMessage = "Failed to initialize session." + ex.Message;
            var initExceptionEntry = new CommandEntry
            {
                Command = "(init)",
                Output = errorMessage,
                Error = errorMessage,
                IsSuccess = false
            };

            terminal.CommandHistory.Add(initExceptionEntry);
            CollectHistoryForType(type, initExceptionEntry);
        }

        await PersistAsync(cancellationToken);
        RaiseChanged();

        return terminal;
    }


    public async Task CloseAsync(Guid terminalId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var terminal = _terminals.FirstOrDefault(t => t.Id == terminalId);
        if (terminal == null)
            return;

        // Collect history before removing
        CollectHistoryForType(terminal.Type, terminal.CommandHistory);

        // Close session if active
        if (!string.IsNullOrEmpty(terminal.SessionId))
        {
            try
            {
                var driver = _driverFactory.GetDriver(terminal.Type);
                await driver.CloseSessionAsync(terminal.SessionId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to close terminal session for {TerminalId}", terminalId);
            }
        }

        _terminals.Remove(terminal);
        _terminalExecutionLocks.Remove(terminalId);
        ResetHistoryNavigation(terminalId);

        if (_activeTerminalId == terminalId)
        {
            _activeTerminalId = _terminals.FirstOrDefault()?.Id ?? Guid.Empty;
        }

        await PersistAsync(cancellationToken);
        RaiseChanged();
    }

    public async Task SetActiveAsync(Guid terminalId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var terminal = _terminals.FirstOrDefault(t => t.Id == terminalId);
        if (terminal == null)
            return;

        _activeTerminalId = terminalId;
        if (terminal.IsMinimized)
            terminal.IsMinimized = false;

        RaiseChanged();
    }

    public async Task SetMinimizedAsync(Guid terminalId, bool minimized, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var terminal = _terminals.FirstOrDefault(t => t.Id == terminalId);
        if (terminal == null)
            return;

        terminal.IsMinimized = minimized;

        if (!minimized)
            _activeTerminalId = terminalId;
    
        await PersistAsync(cancellationToken);
        RaiseChanged();
    }

    public async Task<CommandEntry?> ExecuteAsync(Guid terminalId, string command, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var terminal = _terminals.FirstOrDefault(t => t.Id == terminalId);
        if (terminal == null)
            return null;

        if (string.IsNullOrWhiteSpace(command))
            return null;

        var trimmedCommand = command.Trim();
        var terminalLock = GetExecutionLock(terminalId);

        await terminalLock.WaitAsync(cancellationToken);
        try
        {
            ResetHistoryNavigation(terminalId);

            var driver = _driverFactory.GetDriver(terminal.Type);

            var result = await driver.ExecuteCommandAsync(terminal.SessionId!, trimmedCommand, cancellationToken);

            var entry = new CommandEntry
            {
                Command = trimmedCommand,
                Output = result.Data?.Result ?? result.Data?.Data?.ToString() ?? result.Message ?? result.Error?.Message ?? string.Empty,
                Error = result.Data?.Error,
                IsSuccess = result.IsSuccess
            };

            if (result.Data?.NewPrompt != null)
                terminal.Prompt = result.Data.NewPrompt;

            terminal.CommandHistory.Add(entry);
            CollectHistoryForType(terminal.Type, entry);

            await PersistAsync(cancellationToken);
            RaiseChanged();

            return entry;
        }
        catch (Exception ex)
        {
            var errorEntry = new CommandEntry
            {
                Command = trimmedCommand,
                Output = string.Empty,
                Error = ex.Message,
                IsSuccess = false
            };

            terminal.CommandHistory.Add(errorEntry);
            CollectHistoryForType(terminal.Type, errorEntry);
            await PersistAsync(cancellationToken);
            RaiseChanged();

            return errorEntry;
        }
        finally
        {
            terminalLock.Release();
        }
    }

    public string NavigateHistory(Guid terminalId, int direction, string currentInput)
    {
        var terminal = _terminals.FirstOrDefault(t => t.Id == terminalId);
        if (terminal?.CommandHistory == null || terminal.CommandHistory.Count == 0)
            return currentInput;

        if (!_historyIndexByTerminalId.TryGetValue(terminalId, out var historyIndex))
            historyIndex = -1;

        if (historyIndex == -1 && direction > 0)
        {
            // Capture draft before navigating history (mimics typical terminal behavior)
            _draftInputByTerminalId[terminalId] = currentInput;
        }

        historyIndex += direction;

        if (historyIndex < -1)
            historyIndex = -1;
        else if (historyIndex >= terminal.CommandHistory.Count)
            historyIndex = terminal.CommandHistory.Count - 1;

        _historyIndexByTerminalId[terminalId] = historyIndex;

        if (historyIndex >= 0)
            return terminal.CommandHistory[^((historyIndex) + 1)].Command;

        return _draftInputByTerminalId.TryGetValue(terminalId, out var draft) ? draft : string.Empty;
    }

    public void ResetHistoryNavigation(Guid terminalId)
    {
        _historyIndexByTerminalId[terminalId] = -1;
        _draftInputByTerminalId.Remove(terminalId);
    }

    public Task PersistAsync(CancellationToken cancellationToken = default)
    {
        return PersistInternalAsync(cancellationToken);
    }

    private async Task PersistInternalAsync(CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken);
        await _terminalStorage.SaveAsync(_terminals);
    }

    private void RaiseChanged() => _changed?.Invoke();

    private async Task ValidateLoadedSessionsAsync(CancellationToken cancellationToken)
    {
        foreach (var terminal in _terminals.Where(t => !string.IsNullOrEmpty(t.SessionId)))
        {
            try
            {
                var driver = _driverFactory.GetDriver(terminal.Type);
                terminal.IsValid = await driver.ValidateSessionAsync(terminal.SessionId!, cancellationToken);
            }
            catch
            {
                terminal.IsValid = false;
            }
        }
    }

    private List<CommandEntry> GetSeedHistoryForType(TerminalType type)
    {
        if (!_historyByType.TryGetValue(type, out var history) || history.Count == 0)
            return new List<CommandEntry>();

        // Copy references (CommandEntry is immutable init-only)
        return new List<CommandEntry>(history);
    }

    private void CollectHistoryForType(TerminalType type, CommandEntry entry)
    {
        if (!_historyByType.TryGetValue(type, out var history))
        {
            history = new List<CommandEntry>();
            _historyByType[type] = history;
        }

        history.Add(entry);
        TrimHistory(history);
    }

    private void CollectHistoryForType(TerminalType type, List<CommandEntry> entries)
    {
        if (entries.Count == 0)
            return;

        if (!_historyByType.TryGetValue(type, out var history))
        {
            history = new List<CommandEntry>();
            _historyByType[type] = history;
        }

        history.AddRange(entries);
        TrimHistory(history);
    }

    private void TrimHistory(List<CommandEntry> history)
    {
        if (history.Count <= MaxSeededHistoryEntriesPerType)
            return;

        var removeCount = history.Count - MaxSeededHistoryEntriesPerType;
        history.RemoveRange(0, removeCount);
    }

    private void RebuildHistoryCacheFromInstances()
    {
        _historyByType.Clear();

        foreach (var terminal in _terminals)
        {
            CollectHistoryForType(terminal.Type, terminal.CommandHistory);
        }
    }

    private SemaphoreSlim GetExecutionLock(Guid terminalId)
    {
        if (_terminalExecutionLocks.TryGetValue(terminalId, out var existing))
            return existing;

        var created = new SemaphoreSlim(1, 1);
        _terminalExecutionLocks[terminalId] = created;
        return created;
    }
}
