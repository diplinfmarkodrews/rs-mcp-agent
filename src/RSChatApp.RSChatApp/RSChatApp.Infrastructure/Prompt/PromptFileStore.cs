using System.Collections.Immutable;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace RSChatApp.Infrastructure.Prompt;

public sealed class PromptFileStore : IPromptFileStore, IDisposable
{
    private const string SystemPromptFileName = "SystemPrompt.md";
    private const string SuggestionPromptFileName = "SuggestionPrompt.md";

    private static readonly ImmutableDictionary<string, string> DefaultPromptFiles =
        ImmutableDictionary.CreateRange(StringComparer.OrdinalIgnoreCase, new[]
        {
            new KeyValuePair<string, string>(PromptNames.SystemPrompt, SystemPromptFileName),
            new KeyValuePair<string, string>(PromptNames.SuggestionPrompt, SuggestionPromptFileName),
        });

    private readonly ILogger<PromptFileStore> _logger;
    private readonly IFileProvider _fileProvider;
    private readonly IDisposable _reloadSubscription;

    private volatile ImmutableDictionary<string, string> _prompts =
        ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase);

    public PromptFileStore(IWebHostEnvironment environment, ILogger<PromptFileStore> logger)
    {
        _logger = logger;

        var promptsPath = Path.Combine(environment.ContentRootPath, "Prompts");
        Directory.CreateDirectory(promptsPath);
        _fileProvider = new PhysicalFileProvider(promptsPath);

        LoadPrompts();

        _reloadSubscription = ChangeToken.OnChange(
            changeTokenProducer: () => _fileProvider.Watch("*.md"),
            changeTokenConsumer: LoadPrompts);
    }

    public string GetRequired(string name)
    {
        if (TryGet(name, out var prompt) && prompt is not null)
        {
            return prompt;
        }

        throw new KeyNotFoundException($"Prompt '{name}' not found in Prompts folder.");
    }

    public bool TryGet(string name, out string? prompt)
    {
        if (_prompts.TryGetValue(name, out var value))
        {
            prompt = value;
            return true;
        }

        prompt = null;
        return false;
    }

    public IReadOnlyDictionary<string, string> GetAll() => _prompts;

    private void LoadPrompts()
    {
        try
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (promptName, fileName) in DefaultPromptFiles)
            {
                var fileInfo = _fileProvider.GetFileInfo(fileName);
                if (!fileInfo.Exists)
                {
                    _logger.LogWarning("Prompt file missing: {PromptName} -> {FileName}", promptName, fileName);
                    continue;
                }

                using var stream = fileInfo.CreateReadStream();
                using var reader = new StreamReader(stream);
                builder[promptName] = reader.ReadToEnd();
            }

            _prompts = builder.ToImmutable();

            _logger.LogInformation("Loaded {PromptCount} prompts from Prompts folder", _prompts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load prompts");
        }
    }

    public void Dispose()
    {
        _reloadSubscription.Dispose();
        if (_fileProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
