using Marten;
using RSChatApp.Application.Core.Chat.Commands;
using RSChatApp.Domain.Chat.ToolCall;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.ToolCallConfirmation;

public static class RejectToolResultHandler
{
    public static async Task Handle(
        RejectToolResultCommand command,
        IDocumentSession documentSession,
        CancellationToken ct)
    {
        var doc = await documentSession.LoadAsync<ToolCallDocument>(command.ToolCallDocumentId, ct);
        if (doc is null) return;

        documentSession.Store(doc with
        {
            Status = ToolCallStatus.Rejected,
            CompletedAt = DateTime.UtcNow
        });
    }
}

