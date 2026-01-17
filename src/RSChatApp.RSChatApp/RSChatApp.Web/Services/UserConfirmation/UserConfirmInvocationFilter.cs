using Microsoft.SemanticKernel;
using RSChatApp.Infrastructure.UserInteraction;

namespace RSChatApp.Web.Services.UserConfirmation;

public sealed class UserConfirmInvocationFilter : IFunctionInvocationFilter
{
    private readonly IWaitForUserInteraction<TerminalConfirmRequest, UserConfirmationResult> _ui;

    public UserConfirmInvocationFilter(IWaitForUserInteraction<TerminalConfirmRequest, UserConfirmationResult> ui)
        => _ui = ui;

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext ctx, Func<FunctionInvocationContext, Task> next)
    {
        if (TryCreateTerminalConfirmationRequest(ctx, out var request))
        {
            var decision = await _ui.RequestUserInteractionAsync(request);
            if (decision.Result != UserConfirmationResultEnum.Confirmed)
            {
                ctx.Result = new FunctionResult(ctx.Function, $"User {decision.Result} execution.");
                return;
            }
        }

        await next(ctx);
    }

    private static bool TryCreateTerminalConfirmationRequest(FunctionInvocationContext ctx, out TerminalConfirmRequest request)
    {
        request = default!;

        // Default to allow-list style: only confirm clearly terminal command executions.
        var plugin = ctx.Function.Metadata.PluginName ?? string.Empty;
        var function = ctx.Function.Metadata.Name ?? ctx.Function.Name ?? string.Empty;

        var normalized = NormalizeToolName($"{plugin}.{function}");
        if (!normalized.Contains("terminal", StringComparison.Ordinal) || !normalized.Contains("executecommand", StringComparison.Ordinal))
        {
            return false;
        }

        // Extract command argument (common spellings).
        var command = TryGetArgAsString(ctx.Arguments, "command")
                      ?? TryGetArgAsString(ctx.Arguments, "cmd");
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        request = new TerminalConfirmRequest(
            ToolName: string.IsNullOrWhiteSpace(plugin) ? function : $"{plugin}.{function}",
            Command: command,
            Language: "bash");

        return true;
    }

    private static string? TryGetArgAsString(KernelArguments? args, string key)
    {
        if (args is null)
        {
            return null;
        }

        if (!args.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string s => s,
            _ => value.ToString()
        };
    }

    private static string NormalizeToolName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        // Lowercase alphanumerics only, strip "Async" suffix.
        var canonical = name.Trim();
        if (canonical.EndsWith("Async", StringComparison.Ordinal))
        {
            canonical = canonical[..^5];
        }

        Span<char> buffer = stackalloc char[canonical.Length];
        var idx = 0;
        foreach (var ch in canonical)
        {
            if (char.IsLetterOrDigit(ch))
            {
                buffer[idx++] = char.ToLowerInvariant(ch);
            }
        }

        return new string(buffer[..idx]);
    }
}

public record TerminalConfirmRequest(string ToolName, string Command, string Language = "bash");

public record UserConfirmationResult(UserConfirmationResultEnum Result);

public enum UserConfirmationResultEnum
{
    Confirmed = 1,
    Skipped = 2,
    Cancelled = 3
}