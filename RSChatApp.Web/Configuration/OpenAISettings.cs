using Microsoft.IdentityModel.Protocols.Configuration;

public class OpenAISettings
{
    public string Model { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty; // optional
    public string ApiKey { get; set; } = string.Empty;
    // in Settings we configure only the name of the Env variable
    // fetch it at Startup
    public void SetApiKey()
    {
        ApiKey = Environment.GetEnvironmentVariable(ApiKey) ?? string.Empty;
            
    }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Model) && !string.IsNullOrEmpty(Address) && !string.IsNullOrEmpty(ApiKey);
    }
}
