using System.Runtime.CompilerServices;

namespace RSChatApp.Application.Core.Chat;

public static class PausableAsyncEnumerable
{
    public static async IAsyncEnumerable<T> WithPauseControl<T>(
        this IAsyncEnumerable<T> source,
        IPausableStreamControl control,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(control.Token, cancellationToken);

        await foreach (var item in source.WithCancellation(linked.Token))
        {
            // Block while paused; throws if cancelled
            while (control.IsPaused)
            {
                linked.Token.ThrowIfCancellationRequested();
                await Task.Delay(50, linked.Token);
            }

            yield return item;
        }
    }
}

