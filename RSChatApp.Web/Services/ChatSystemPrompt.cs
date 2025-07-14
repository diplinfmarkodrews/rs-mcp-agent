namespace RSChatApp.Web.Services;

public class ChatSystemPrompt(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;
    public string SystemPrompt()
    {
        return _configuration["ChatSystemPrompt"] ?? "You are a helpful assistant.";
    }
}