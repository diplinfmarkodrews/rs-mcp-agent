namespace RSChatApp.Infrastructure.UserInteraction;

public interface IWaitForUserInteraction<TRequest, TResult>
{
        /// <summary>
        /// Requests user interaction and waits for the result
        /// </summary>
        /// <param name="request">The user interaction request details</param>
        /// <returns>Returns result Type T of userinteraction</returns>
        Task<TResult> RequestUserInteractionAsync(TRequest request);
    
        /// <summary>
        /// Event raised when user interaction is requested
        /// </summary>
        event EventHandler<(TRequest Request, TaskCompletionSource<TResult> TaskCompletionSource)> UserInteractionRequested;
        
}


public class WaitForUserInteraction<TRequest, TResult> : IWaitForUserInteraction<TRequest, TResult>
{
    public event EventHandler<(TRequest Request, TaskCompletionSource<TResult> TaskCompletionSource)>? UserInteractionRequested;
    
    public async Task<TResult> RequestUserInteractionAsync(TRequest request)
    {
        // Important for Blazor Server: avoid running the awaiting continuation inline on the UI thread
        // when the result is set from a UI event handler.
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        // Raise the event to notify MainLayout to show the modal
        UserInteractionRequested?.Invoke(this, (request, tcs));
        
        // Wait for the result (either success, failure, or cancellation)
        return await tcs.Task.ConfigureAwait(false);
    }
}