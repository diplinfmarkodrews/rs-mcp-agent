using RSChatApp.Web.Filter.UserConfirmation;

namespace RSChatApp.Web.Models.Chat.UserConfirmation;

public class UserConfirmationToolCall
{
    public UserConfirmationResultEnum Result { get; init; }
    public IDictionary<string, object?>? Arguments { get; init; }
    public UserConfirmationToolCall(UserConfirmationResultEnum result, IDictionary<string, object?> arguments = null)
    {
        Result = result;
        Arguments = arguments;
    }
    
    public static UserConfirmationToolCall Cancelled 
        => new(UserConfirmationResultEnum.Cancelled);
    
    public static UserConfirmationToolCall Skipped 
        => new(UserConfirmationResultEnum.Skipped);
}
