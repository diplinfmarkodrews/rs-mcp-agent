namespace RSChatApp.Domain.ValueObjects;

public class ChatRole : SmartEnum<ChatRole>
{
    public static readonly ChatRole NotSet = new(nameof(NotSet), 0);
    public static readonly ChatRole User = new(nameof(User), 1);
    public static readonly ChatRole Assistant = new(nameof(Assistant), 2);
    public static readonly ChatRole System = new(nameof(System), 3);
    public static readonly ChatRole Tool = new(nameof(Tool), 4);
    protected ChatRole(string name, int value) : base(name, value) { }
}
