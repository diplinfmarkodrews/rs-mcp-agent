using Marten;
using RSChatApp.Application.Core.Chat;
using RSChatApp.Domain.Chat.Message;
using RSChatApp.Domain.Chat.ModelSettings;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.SendMessage;

public static class SendMessageHandler
{
    public static async Task<IEnumerable<object>> Handle(
        SendMessageCommand command,
        IDocumentSession documentSession,
        IEventStoreRepository<MessageAggregate> repository,
        CancellationToken ct)
    {
        var settings = command.AiChatRequest.Settings;

        var modelSettingsId = await ResolveModelSettingsId(
            documentSession, command.SessionId, settings, ct);

        var aggregate = MessageAggregate.Create(
            command.Id,
            command.SessionId,
            command.SenderId,
            command.Content,
            command.Role,
            MessageType.TextFull,
            null, // User message, message id and author name will not be set
            null,
            modelSettingsId);

        repository.Save(aggregate);

        return aggregate.DequeueUncommittedEvents();
    }

    private static async Task<Guid> ResolveModelSettingsId(
        IDocumentSession documentSession,
        Guid sessionId,
        AiChatSettings settings,
        CancellationToken ct)
    {
        var candidate = new ModelSettingsDocument
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            ServiceId = settings.ServiceId,
            ModelId = settings.ModelId,
            IsPrivate = settings.IsPrivate,
            ActiveToolNames = settings.ActiveToolNames.ToList(),
            ExecutionSettings = settings.PromptExecutionSettings,
            CreatedAt = DateTime.UtcNow
        };

        var existing = await documentSession.Query<ModelSettingsDocument>()
            .FirstOrDefaultAsync(x => x.SessionId == sessionId, ct);

        if (existing is not null && existing.EquivalentTo(candidate))
            return existing.Id;

        documentSession.Store(candidate);
        return candidate.Id;
    }
}
