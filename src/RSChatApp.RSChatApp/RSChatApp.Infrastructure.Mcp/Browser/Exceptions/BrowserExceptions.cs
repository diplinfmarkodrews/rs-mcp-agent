namespace RSChatApp.Mcp.Browser.Exceptions;

public class NavigationException : Exception
{
    public NavigationException(string message) : base(message) { }
    public NavigationException(string message, Exception innerException) : base(message, innerException) { }
}

public class InteractionException : Exception
{
    public InteractionException(string message) : base(message) { }
    public InteractionException(string message, Exception innerException) : base(message, innerException) { }
}
