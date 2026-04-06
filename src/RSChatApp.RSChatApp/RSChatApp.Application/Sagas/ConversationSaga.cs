using Marten;
using RSChatApp.Application.Core.Chat;
using RSChatApp.Application.Core.Chat.Commands;
using RSChatApp.Application.Core.Chat.Dtos;
using RSChatApp.Application.Core.Chat.Events;
using RSChatApp.Application.Core.Prompt;
using RSChatApp.Domain.Chat.Message;
using RSChatApp.Domain.Chat.Message.Events;
using RSChatApp.Domain.Chat.ModelSettings;
using RSChatApp.Domain.Chat.Session.Events;
using RSChatApp.Domain.ValueObjects;
using Wolverine.Persistence.Sagas;

namespace RSChatApp.Application.Sagas;

public class ConversationSaga : Saga
{
    public Guid Id { get; set; }
    public UserId UserId { get; set; }
    public Guid? ActiveRequestId { get; set; }
    public Guid? ActiveMessageId { get; set; }
    public Guid? ModelSettingsId { get; set; }
    public Guid? ParentSessionId { get; set; }
    public AiChatRequest? LastAiChatRequest { get; set; }
    public Dictionary<Guid, Guid> PendingToolCallConfirmations { get; set; } = new();
    public Dictionary<Guid, Guid> PendingToolResultConfirmations { get; set; } = new();

    // ── Lifecycle ──────────────────────────────────────────────────────

    public static ConversationSaga Start(SessionCreatedEvent e)
        => new() { Id = e.Id, UserId = e.UserId, ParentSessionId = e.ParentSessionId };

    // ── User message → assistant response ──────────────────────────────

    public async Task Handle(
        [SagaIdentityFrom(nameof(MessageCreatedEvent.SessionId))] MessageCreatedEvent message,
        IPromptBuilder promptBuilder,
        IMessageContext context,
        IDocumentSession documentSession,
        IEventStoreRepository<MessageAggregate> repository,
        CancellationToken cancellationToken)
    {
        if (message.Role != ChatRole.User || string.IsNullOrWhiteSpace(message.Content))
            return;

        // 1. Load model settings
        var settings = await LoadModelSettingsAsync(documentSession, message.ModelSettingsId, cancellationToken);

        var aiChatSettings = new AiChatSettings(
            settings?.ServiceId ?? string.Empty,
            settings?.ActiveToolNames ?? [],
            settings?.IsPrivate ?? true,
            IsLocal: false, // we need automatic evaluation from model settings
            settings?.ExecutionSettings ?? AiChatPromptExecutionSettings.Default,
            settings?.ModelId);

        var aiChatRequest = new AiChatRequest(
            message.SessionId,
            new AiChatRequestMessage([new ChatMessageDto(message.Role, message.Content)]),
            aiChatSettings);

        // 2. Persist user message aggregate
        var userMessage = MessageAggregate.Create(
            Guid.NewGuid(),
            message.SessionId, message.SenderId, message.Content,
            message.Role, MessageType.TextFull,
            null, null, message.ModelSettingsId);

        repository.Save(userMessage);

        // 3. Store saga state for recovery / retry
        ModelSettingsId = message.ModelSettingsId;
        LastAiChatRequest = aiChatRequest;

        // 4. Create assistant message and dispatch LLM request
        await DispatchLlmRequest(message.SessionId, aiChatRequest, context, repository, cancellationToken);
    }

    // ── LLM completed ──────────────────────────────────────────────────

    public void Handle(
        [SagaIdentityFrom(nameof(LlmResponseCompletedEvent.SessionId))] LlmResponseCompletedEvent message)
    {
        if (ActiveRequestId != message.RequestId)
            return;

        ClearActiveRequest();
        LastAiChatRequest = null;
    }

    // ── LLM gave up ───────────────────────────────────────────────────

    public void Handle(
        [SagaIdentityFrom(nameof(LlmResponseGaveUpEvent.SessionId))] LlmResponseGaveUpEvent message)
    {
        if (ActiveRequestId != message.RequestId)
            return;

        ClearActiveRequest();
        // LastAiChatRequest preserved for retry
    }

    // ── Tool call confirmation ─────────────────────────────────────────

    public void Handle(
        [SagaIdentityFrom(nameof(ToolCallConfirmationRequestedEvent.SessionId))] ToolCallConfirmationRequestedEvent message)
    {
        if (!ActiveRequestId.HasValue) return;
        PendingToolCallConfirmations[message.ToolCallDocumentId] = ActiveRequestId.Value;
    }

    public void Handle(
        [SagaIdentityFrom(nameof(ConfirmToolCallCommand.SessionId))] ConfirmToolCallCommand command)
    {
        PendingToolCallConfirmations.Remove(command.ToolCallDocumentId);
    }

    public async Task Handle(
        [SagaIdentityFrom(nameof(RejectToolCallCommand.SessionId))] RejectToolCallCommand command,
        IMessageContext context)
    {
        if (!PendingToolCallConfirmations.Remove(command.ToolCallDocumentId, out var requestId))
            return;

        await context.PublishAsync(new LlmResponseGaveUpEvent(
            requestId, Id, UserId, GaveUpReasons.ToolCallRejected));
    }

    // ── Tool result confirmation ───────────────────────────────────────

    public void Handle(
        [SagaIdentityFrom(nameof(ToolResultConfirmationRequestedEvent.SessionId))] ToolResultConfirmationRequestedEvent message)
    {
        if (!ActiveRequestId.HasValue) return;
        PendingToolResultConfirmations[message.ToolCallDocumentId] = ActiveRequestId.Value;
    }

    public void Handle(
        [SagaIdentityFrom(nameof(ConfirmToolResultCommand.SessionId))] ConfirmToolResultCommand command)
    {
        PendingToolResultConfirmations.Remove(command.ToolCallDocumentId);
    }

    public async Task Handle(
        [SagaIdentityFrom(nameof(RejectToolResultCommand.SessionId))] RejectToolResultCommand command,
        IMessageContext context)
    {
        if (!PendingToolResultConfirmations.Remove(command.ToolCallDocumentId, out var requestId))
            return;

        await context.PublishAsync(new LlmResponseGaveUpEvent(
            requestId, Id, UserId, GaveUpReasons.ToolResultRejected));
    }

    public void Handle(
        [SagaIdentityFrom(nameof(RedactToolResultCommand.SessionId))] RedactToolResultCommand command)
    {
        PendingToolResultConfirmations.Remove(command.ToolCallDocumentId);
    }

    // ── Cancel / Pause / Resume ────────────────────────────────────────

    public void Handle(
        [SagaIdentityFrom(nameof(CancelGenerationCommand.SessionId))] CancelGenerationCommand command,
        IActiveRequestRegistry registry)
    {
        if (!ActiveRequestId.HasValue) return;
        registry.Cancel(ActiveRequestId.Value);
    }

    public void Handle(
        [SagaIdentityFrom(nameof(PauseGenerationCommand.SessionId))] PauseGenerationCommand command,
        IActiveRequestRegistry registry)
    {
        if (!ActiveRequestId.HasValue) return;
        registry.Pause(ActiveRequestId.Value);
    }

    public void Handle(
        [SagaIdentityFrom(nameof(ResumeGenerationCommand.SessionId))] ResumeGenerationCommand command,
        IActiveRequestRegistry registry)
    {
        if (!ActiveRequestId.HasValue) return;
        registry.Resume(ActiveRequestId.Value);
    }

    // ── Retry ──────────────────────────────────────────────────────────

    public async Task Handle(
        [SagaIdentityFrom(nameof(RetryGenerationCommand.SessionId))] RetryGenerationCommand command,
        IMessageContext context,
        IEventStoreRepository<MessageAggregate> repository,
        CancellationToken cancellationToken)
    {
        if (ActiveRequestId.HasValue || LastAiChatRequest is null)
            return;

        await DispatchLlmRequest(Id, LastAiChatRequest, context, repository, cancellationToken);
    }

    // ── Session deleted ────────────────────────────────────────────────

    public async Task Handle(
        [SagaIdentityFrom(nameof(SessionDeletedEvent.Id))] SessionDeletedEvent message,
        IActiveRequestRegistry registry,
        IMessageContext context)
    {
        if (ActiveRequestId.HasValue)
        {
            registry.Cancel(ActiveRequestId.Value);
            await context.PublishAsync(new LlmResponseGaveUpEvent(
                ActiveRequestId.Value, message.Id, UserId, GaveUpReasons.SessionDeleted));
        }

        MarkCompleted();
    }

    // ── Private helpers ────────────────────────────────────────────────

    private async Task DispatchLlmRequest(
        Guid sessionId,
        AiChatRequest aiChatRequest,
        IMessageContext context,
        IEventStoreRepository<MessageAggregate> repository,
        CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var assistantMessage = MessageAggregate.Create(
            messageId, sessionId, UserId, null,
            ChatRole.Assistant, MessageType.TextDelta,
            null, null);

        repository.Save(assistantMessage);

        ActiveRequestId = requestId;
        ActiveMessageId = messageId;

        await context.PublishAsync(new LlmResponseRequestedEvent(
            requestId, sessionId, messageId, UserId, aiChatRequest));
    }

    private void ClearActiveRequest()
    {
        ActiveRequestId = null;
        ActiveMessageId = null;
        PendingToolCallConfirmations.Clear();
        PendingToolResultConfirmations.Clear();
    }

    private static async Task<ModelSettingsDocument?> LoadModelSettingsAsync(
        IDocumentSession documentSession, Guid? modelSettingsId, CancellationToken ct)
    {
        if (!modelSettingsId.HasValue) return null;
        return await documentSession.LoadAsync<ModelSettingsDocument>(modelSettingsId.Value, ct);
    }
}
