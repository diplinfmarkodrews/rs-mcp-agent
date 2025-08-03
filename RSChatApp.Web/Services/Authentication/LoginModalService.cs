namespace RSChatApp.Web.Services.Authentication;

/// <summary>
/// Service for managing login modal state across components
/// </summary>
public interface ILoginModalService
{
    /// <summary>
    /// Requests to show the login modal and waits for the result
    /// </summary>
    /// <returns>True if login was successful, false if cancelled or failed</returns>
    Task<bool> RequestLoginAsync();
    
    /// <summary>
    /// Event raised when login is requested
    /// </summary>
    event EventHandler<TaskCompletionSource<bool>> LoginRequested;
}

/// <summary>
/// Implementation of login modal service
/// </summary>
public class LoginModalService : ILoginModalService
{
    public event EventHandler<TaskCompletionSource<bool>>? LoginRequested;

    public async Task<bool> RequestLoginAsync()
    {
        var tcs = new TaskCompletionSource<bool>();
        
        // Raise the event to notify MainLayout to show the modal
        LoginRequested?.Invoke(this, tcs);
        
        // Wait for the result (either success, failure, or cancellation)
        return await tcs.Task;
    }
}
