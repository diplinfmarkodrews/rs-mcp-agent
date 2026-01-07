using RSChatApp.Common.Exceptions;

namespace ReportServer.Abstraction.Exceptions;

public class ServerCallFailedException : SerializableException
{
    public string ServerException { get; init; }
    public ServerCallFailedException(string message) : base(message)
    {
    }
    public ServerCallFailedException(string message, string serverException) : base(message)
    {
        ServerException = serverException;
    }

}