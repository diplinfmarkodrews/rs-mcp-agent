// using RSChatApp.Application.Core.Prompt;
// using RSChatApp.Application.Features.Message.Events;
// using RSChatApp.Application.Services;
// using RSChatApp.Domain.Message;
// using RSChatApp.Domain.Message.Events;
// using RSChatApp.Domain.Session.Events;
// using RSChatApp.Domain.ValueObjects;
// using Wolverine.Persistence.Sagas;
//
// namespace RSChatApp.Application.Sagas;
//
// public class ConversationSaga : Saga
// {
//// Not used, since only 1 service
//     public Guid Id { get; set; }
//     public UserId UserId { get; set; }
//     public bool IsProcessing { get; set; }
//     public Guid? ActiveRequestId { get; set; }
//     public Queue<Guid> PendingMessageIds { get; set; } = new();
//
//     public static ConversationSaga Start(SessionCreatedEvent e)
//         => new() { Id = e.Id, UserId = e.UserId };
//
//     public async Task Handle(
//         [SagaIdentityFrom(nameof(MessageCreatedEvent.SessionId))] MessageCreatedEvent message,
//         IPromptBuilder promptBuilder,
//         IMessageContext context,
//         CancellationToken cancellationToken)
//     {
//         if (message.Role != ChatRole.User)
//             return;
//
//         if (!IsProcessing)
//             await StartProcessing(message.Id, message.SessionId, message.SenderId, promptBuilder, context, cancellationToken);
//         else
//             PendingMessageIds.Enqueue(message.Id);
//     }
//
//     public async Task Handle(
//         [SagaIdentityFrom(nameof(LlmResponseCompletedEvent.SessionId))] LlmResponseCompletedEvent message,
//         IEventStoreRepository<MessageAggregate> repository,
//         IPromptBuilder promptBuilder,
//         IMessageContext context,
//         CancellationToken cancellationToken)
//     {
//         if (ActiveRequestId != message.RequestId)
//             return;
//
//         var aiMessage = MessageAggregate.Create(
//             Guid.NewGuid(),
//             message.SessionId,
//             message.UserId,
//             message.FullResponse,
//             ChatRole.Assistant);
//
//         repository.Save(aiMessage);
//         ActiveRequestId = null;
//
//         if (PendingMessageIds.TryDequeue(out Guid requestId))
//             await StartProcessing(requestId, Id, UserId, promptBuilder, context, cancellationToken);
//         else
//             IsProcessing = false;
//     }
//
//     // AiService gave up — unblock the saga so new user messages can still be processed
//     public async Task Handle(
//         [SagaIdentityFrom(nameof(LlmResponseGaveUpEvent.SessionId))] LlmResponseGaveUpEvent message)
//     {
//         if (ActiveRequestId != message.RequestId)
//             return;
//
//         ActiveRequestId = null;
//         IsProcessing = false;
//
//         // Drain pending queue — they won't get responses either, discard or re-enqueue
//         PendingMessageIds.Clear();
//     }
//     public async Task Handle(
//      [SagaIdentityFrom(nameof(SessionDeletedEvent.Id))] SessionDeletedEvent message,
//      IMessageContext context)
//     {
//         if (IsProcessing && ActiveRequestId.HasValue)
//         {
//             await context.PublishAsync(new LlmResponseGaveUpEvent(
//                 ActiveRequestId.Value,
//                 message.Id,
//                 UserId,
//                 GaveUpReasons.SessionDeleted));
//         }
//
//         MarkCompleted();
//     }
//
//     private async Task StartProcessing(
//         Guid requestId,
//         Guid sessionId,
//         UserId userId,
//         IPromptBuilder promptBuilder,
//         IMessageContext context,
//         CancellationToken cancellationToken)
//     {
//         IsProcessing = true;
//         ActiveRequestId = requestId;
//
//         var messages = await promptBuilder.BuildAsync(sessionId, cancellationToken);
//
//         await context.PublishAsync(new LlmResponseRequestedEvent(
//             ActiveRequestId.Value,
//             sessionId,
//             userId,
//             messages));
//     }
// }