using Marten;
using RSChatApp.Application.Core.Chat.Commands;
using RSChatApp.Domain.Chat.ToolCall;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.ToolCallConfirmation;

public static class ConfirmToolCallHandler
{
    public static async Task Handle(
        ConfirmToolCallCommand command,
        IDocumentSession documentSession,
        CancellationToken ct)
    {
        var doc = await documentSession.LoadAsync<ToolCallDocument>(command.ToolCallDocumentId, ct);
        if (doc is null) return;

        var updated = doc with { Status = ToolCallStatus.Executing };

        if (command.EditedArguments is not null)
        {
            updated = updated with
            {
                Arguments = new Dictionary<string, object>(
                    command.EditedArguments
                        .Where(kvp => kvp.Value is not null)
                        .Select(kvp => new KeyValuePair<string, object>(kvp.Key, kvp.Value!)))
            };
        }

        documentSession.Store(updated);
    }
}

