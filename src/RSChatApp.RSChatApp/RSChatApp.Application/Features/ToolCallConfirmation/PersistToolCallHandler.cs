using Marten;
using RSChatApp.Application.Core.Chat;
using RSChatApp.Application.Core.Chat.Events;
using RSChatApp.Domain.Chat.ToolCall;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.ToolCallConfirmation;

public static class PersistToolCallHandler
{
    public static async Task<IEnumerable<object>> Handle(
        LlmToolCallEvent message,
        IDocumentSession documentSession,
        IToolCallConfirmationPolicy policy)
    {
        var doc = new ToolCallDocument
        {
            Id = Guid.NewGuid(),
            MessageId = message.MessageId,
            SessionId = message.SessionId,
            ToolName = message.ToolName,
            Arguments = new Dictionary<string, object>(message.Arguments),
            Status = ToolCallStatus.Requested,
            RequestedAt = DateTime.UtcNow
        };

        var autoConfirm = policy.ShouldAutoConfirmCall(message.ToolName, isLocalModel: false);

        if (autoConfirm)
        {
            doc = doc with { Status = ToolCallStatus.Executing };
        }
        else
        {
            doc = doc with { Status = ToolCallStatus.AwaitingCallConfirmation };
        }

        documentSession.Store(doc);

        if (!autoConfirm)
        {
            return [new ToolCallConfirmationRequestedEvent(
                message.SessionId, doc.Id, doc.ToolName, message.Arguments)];
        }

        return [];
    }
}

