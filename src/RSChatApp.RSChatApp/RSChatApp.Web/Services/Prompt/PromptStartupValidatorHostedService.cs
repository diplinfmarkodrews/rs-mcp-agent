using Microsoft.Extensions.Hosting;

namespace RSChatApp.Web.Services.Prompt;

public sealed class PromptStartupValidatorHostedService : IHostedService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IPromptStore _promptStore;
    private readonly ILogger<Program> _startupLogger;

    public PromptStartupValidatorHostedService(
        IWebHostEnvironment environment,
        IPromptStore promptStore,
        ILogger<Program> startupLogger)
    {
        _environment = environment;
        _promptStore = promptStore;
        _startupLogger = startupLogger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var promptsPath = Path.Combine(_environment.ContentRootPath, "Prompts");

        var missing = PromptNames.Required
            .Where(name => !_promptStore.TryGet(name, out var prompt) || string.IsNullOrWhiteSpace(prompt))
            .ToArray();

        if (missing.Length > 0)
        {
            _startupLogger.LogCritical(
                "Missing required prompts: {MissingPrompts}. Expected files in: {PromptsPath}",
                string.Join(", ", missing),
                promptsPath);

            throw new InvalidOperationException(
                $"Missing required prompts: {string.Join(", ", missing)}. Check folder: {promptsPath}");
        }

        _startupLogger.LogInformation(
            "Prompt startup check OK. Loaded prompts: {PromptNames}",
            string.Join(", ", PromptNames.Required));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
