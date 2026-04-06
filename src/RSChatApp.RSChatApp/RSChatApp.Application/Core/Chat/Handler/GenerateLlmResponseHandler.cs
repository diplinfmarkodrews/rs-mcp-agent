using RSChatApp.Application.Core.Chat.Events;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Core.Chat.Handler;

public class GenerateLlmResponseHandler(IAiChatClient aiChatClient, IActiveRequestRegistry registry)
{
    private const int MaxRetries = 3;

    public async Task Handle(
        LlmResponseRequestedEvent message,
        IMessageContext context,
        CancellationToken ct)
    {
        var control = registry.Register(message.RequestId);
        try
        {
            await HandleCore(message, context, control, ct);
        }
        finally
        {
            registry.Unregister(message.RequestId);
        }
    }

    private async Task HandleCore(
        LlmResponseRequestedEvent message,
        IMessageContext context,
        IPausableStreamControl control,
        CancellationToken ct)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(control.Token, ct);
        try
        {
            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    await GenerateAndPublish(message, context, control, linked.Token);
                    return;
                }
                catch (Exception) when (attempt < MaxRetries)
                {
                    await context.PublishAsync(new LlmResponseRetryingEvent(
                       message.RequestId,
                       message.SessionId,
                       message.UserId));

                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2), linked.Token);
                }
                catch (Exception) when (attempt == MaxRetries)
                {
                    await context.PublishAsync(new LlmResponseGaveUpEvent(
                        message.RequestId,
                        message.SessionId,
                        message.UserId,
                        GaveUpReasons.MaxRetriesExceeded));
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            await context.PublishAsync(new LlmResponseGaveUpEvent(
                message.RequestId,
                message.SessionId,
                message.UserId,
                GaveUpReasons.Cancelled));
            throw;
        }
        catch (Exception)
        {
            await context.PublishAsync(new LlmResponseGaveUpEvent(
                message.RequestId,
                message.SessionId,
                message.UserId,
                GaveUpReasons.LlmError));
        }
    }

    private async Task GenerateAndPublish(
        LlmResponseRequestedEvent message,
        IMessageContext context,
        IPausableStreamControl control,
        CancellationToken ct)
    {
        var contentFlags = 0;
        string? chatMessageId = null;
        string? authorName = null;

        var stream = aiChatClient.GetStreamingResponseAsync(message.AiChatRequest, cancellationToken: ct)
            .WithPauseControl(control, ct);

        await foreach (var update in stream)
        {
            if (update.TextDelta != null)
            {
                contentFlags |= MessageType.TextDelta.Value;

                await context.PublishAsync(
                    new LlmTokenGeneratedEvent(message.RequestId, message.SessionId, message.MessageId,
                        message.UserId, update.TextDelta));
            }

            if (update.ToolCall != null)
            {
                contentFlags |= MessageType.ToolCall.Value;
                
                await context.PublishAsync(
                    new LlmToolCallEvent(message.RequestId, message.SessionId, message.MessageId, message.UserId,
                        update.ToolCall.Name, update.ToolCall.Arguments));
            }

            if (update.ToolResult != null)
            {
                contentFlags |= MessageType.ToolResult.Value;

                await context.PublishAsync(
                    new LlmToolResultEvent(message.RequestId, message.SessionId, message.MessageId, message.UserId,
                        message.AiChatRequest.Settings.IsLocal, update.ToolResult.CallId, update.ToolResult.Result));
            }
        }

        // Promote TextDelta to TextFull on completion
        if ((contentFlags & MessageType.TextDelta.Value) != 0)
        {
            contentFlags = (contentFlags & ~MessageType.TextDelta.Value) | MessageType.TextFull.Value;
        }

        var messageType = MessageType.From(contentFlags == 0 ? MessageType.TextFull.Value : contentFlags);

        await context.PublishAsync(
            new LlmResponseCompletedEvent(
                message.RequestId,
                message.SessionId,
                message.MessageId,
                message.UserId,
                messageType,
                chatMessageId,
                authorName));
    }
}