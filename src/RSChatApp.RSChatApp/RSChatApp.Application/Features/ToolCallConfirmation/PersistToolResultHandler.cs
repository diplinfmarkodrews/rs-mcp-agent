using Marten;
using RSChatApp.Application.Core.Chat;
using RSChatApp.Application.Core.Chat.Events;
using RSChatApp.Domain.Chat.ToolCall;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.ToolCallConfirmation;

public static class PersistToolResultHandler
{
    public static async Task<IEnumerable<object>> Handle(
        LlmToolResultEvent message,
        IDocumentSession documentSession,
        IToolCallConfirmationPolicy policy,
        CancellationToken ct)
    {
        var doc = await documentSession.Query<ToolCallDocument>()
            .FirstOrDefaultAsync(x => x.CallId == message.ToolCallId, ct);

        if (doc is null) return [];

        var autoConfirm = policy.ShouldAutoConfirmResult(doc.ToolName, message.IsLocal);

        var updated = doc with
        {
            Result = message.ToolResult,
            Status = autoConfirm
                ? ToolCallStatus.Completed
                : ToolCallStatus.AwaitingResultConfirmation,
            CompletedAt = autoConfirm ? DateTime.UtcNow : null
        };

        documentSession.Store(updated);

        if (!autoConfirm)
        {
            return [new ToolResultConfirmationRequestedEvent(
                message.SessionId, doc.Id, doc.CallId, doc.ToolName, message.ToolResult)];
        }

        return [];
    }
}

