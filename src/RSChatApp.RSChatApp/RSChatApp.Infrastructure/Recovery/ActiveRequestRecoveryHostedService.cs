using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RSChatApp.Application.Core.Chat.Events;
using RSChatApp.Application.Sagas;
using RSChatApp.Domain.Chat.Message;
using RSChatApp.Domain.ValueObjects;
using Wolverine;

namespace RSChatApp.Infrastructure.Recovery;

public sealed class ActiveRequestRecoveryHostedService(
    IServiceProvider serviceProvider,
    ILogger<ActiveRequestRecoveryHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var querySession = scope.ServiceProvider.GetRequiredService<IQuerySession>();
        var documentSession = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var repository = scope.ServiceProvider.GetRequiredService<IEventStoreRepository<MessageAggregate>>();

        var orphanedSagas = await querySession.Query<ConversationSaga>()
            .Where(s => s.ActiveRequestId != null)
            .ToListAsync(cancellationToken);

        if (orphanedSagas.Count == 0)
        {
            logger.LogInformation("No orphaned active requests found on startup.");
            return;
        }

        logger.LogWarning("Found {Count} orphaned active request(s) on startup. Re-issuing.", orphanedSagas.Count);

        foreach (var saga in orphanedSagas)
        {
            if (saga.LastAiChatRequest is null)
            {
                logger.LogWarning(
                    "Saga {SagaId} has ActiveRequestId {RequestId} but no LastAiChatRequest. Clearing.",
                    saga.Id, saga.ActiveRequestId);

                saga.ActiveRequestId = null;
                saga.ActiveMessageId = null;
                documentSession.Store(saga);
                continue;
            }

            // Create a new assistant message aggregate for the re-issued request
            var newRequestId = Guid.NewGuid();
            var newMessageId = Guid.NewGuid();

            var assistantMessage = MessageAggregate.Create(
                newMessageId, saga.Id, saga.UserId, null,
                ChatRole.Assistant, MessageType.TextDelta,
                null, null);

            repository.Save(assistantMessage);

            // Update saga state
            saga.ActiveRequestId = newRequestId;
            saga.ActiveMessageId = newMessageId;
            saga.PendingToolCallConfirmations.Clear();
            saga.PendingToolResultConfirmations.Clear();
            documentSession.Store(saga);

            logger.LogInformation(
                "Re-issuing LLM request for saga {SagaId}: new RequestId={RequestId}, MessageId={MessageId}",
                saga.Id, newRequestId, newMessageId);

            await bus.PublishAsync(new LlmResponseRequestedEvent(
                newRequestId, saga.Id, newMessageId, saga.UserId, saga.LastAiChatRequest));
        }

        await documentSession.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

