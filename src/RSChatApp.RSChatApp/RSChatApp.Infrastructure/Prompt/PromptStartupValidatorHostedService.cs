using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RSChatApp.Web.Services.Prompt;

namespace RSChatApp.Infrastructure.Prompt;

public sealed class PromptStartupValidatorHostedService : IHostedService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IPromptFileStore _promptFileStore;
    private readonly ILogger _startupLogger;

    public PromptStartupValidatorHostedService(
        IWebHostEnvironment environment,
        IPromptFileStore promptFileStore,
        ILogger startupLogger)
    {
        _environment = environment;
        _promptFileStore = promptFileStore;
        _startupLogger = startupLogger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var promptsPath = Path.Combine(_environment.ContentRootPath, "Prompts");

        var missing = PromptNames.Required
            .Where(name => !_promptFileStore.TryGet(name, out var prompt) || string.IsNullOrWhiteSpace(prompt))
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
