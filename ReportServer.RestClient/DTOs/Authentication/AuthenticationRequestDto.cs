using Newtonsoft.Json;

namespace ReportServer.RestClient.DTOs.Authentication;

public class AuthenticationRequestDto
{
    [JsonProperty("username")]
    public string Username { get; set; }
    
    [JsonProperty("password")]
    public string Password { get; set; }
}