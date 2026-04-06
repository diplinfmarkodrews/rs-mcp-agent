namespace RSChatApp.Application.Core.Chat;

public interface IToolCallConfirmationPolicy
{
    bool ShouldAutoConfirmCall(string toolName, bool isLocalModel);
    bool ShouldAutoConfirmResult(string toolName, bool isLocalModel);
}

public class DefaultToolCallConfirmationPolicy : IToolCallConfirmationPolicy
{
    public bool ShouldAutoConfirmCall(string toolName, bool isLocalModel) => isLocalModel;

    public bool ShouldAutoConfirmResult(string toolName, bool isLocalModel) => isLocalModel;
}

