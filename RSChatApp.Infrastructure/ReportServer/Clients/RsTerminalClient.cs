using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using ReportServer.Abstraction.Contracts;
using ReportServer.Abstraction.Contracts.Terminal;
using RSChatApp.Infrastructure.ReportServer.Terminal;

namespace RSChatApp.Infrastructure.ReportServer.Clients;

public interface IRsTerminalClient
{
    Task<Result<TerminalSessionInfo>> InitTerminalSessionAsync(CancellationToken cancellationToken);

    Task<Result<CommandResult>> ExecuteCommandAsync(string sessionId, string command,
        CancellationToken cancellationToken);

    Task<Result> CloseTerminalSessionAsync(string sessionId, CancellationToken cancellationToken);
}

public class RsTerminalClient : IRsTerminalClient
{
    private readonly ILogger<RsTerminalClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    
    public RsTerminalClient(IHttpClientFactory httpClientFactory, ILogger<RsTerminalClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Result<TerminalSessionInfo>> InitTerminalSessionAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initiating terminal session from RsMcpServer");
        using var httpClient = _httpClientFactory.CreateClient(RsMcpServerHttpClientName.ClientName);
        try
        {
            return await httpClient.GetFromJsonAsync<Result<TerminalSessionInfo>>("api/rs-rest/terminal/init-session", cancellationToken) 
                ?? throw new InvalidDataException("Response was null");
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Failed to init terminal session, reason:  {Reason}", exc.Message);
            return Result<TerminalSessionInfo>.Fail(exc);
        }
    }
    

    public async Task<Result<CommandResult>> ExecuteCommandAsync(string sessionId, string command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing command in rs terminal using sessionId: {SessionId}", sessionId);
        using var httpClient = _httpClientFactory.CreateClient(RsMcpServerHttpClientName.ClientName);
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/rs-rest/terminal/execute-command",
                new { sessionId, command }, cancellationToken);
            
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Result<CommandResult>>(cancellationToken)
                ?? throw new InvalidDataException("Response was null");
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Failed to init terminal session, reason:  {Reason}", exc.Message);
            return Result<CommandResult>.Fail(exc);
        }
    }

    public async Task<Result> CloseTerminalSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Closing terminal session from RsMcpServer with sessionId: {SessionId}", sessionId);
        using var httpClient = _httpClientFactory.CreateClient(RsMcpServerHttpClientName.ClientName);
        try
        {
            return await httpClient.DeleteFromJsonAsync<Result>(
                    new StringBuilder("api/rs-rest/terminal/close-session&sessionId=")
                                .Append(sessionId)
                                .ToString())
                ?? Result.Fail("Response was null");
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Failed to close terminal session, reason: {Reason}", exc.Message);
            return Result.Fail(exc);
        }
    }
    
}