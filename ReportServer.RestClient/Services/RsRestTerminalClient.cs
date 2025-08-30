using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ReportServer.RestClient.DTOs.Terminal;
using ReportServer.RestClient.Infrastructure;
using ReportServer.RestClient.DTOs;

namespace ReportServer.RestClient.Services;

public class RsRestTerminalClient : RsRestClientBase
{
    private readonly ILogger _logger;

    public RsRestTerminalClient(ILogger logger, IHttpClientFactory httpClientFactory, CookieContainerProvider cookieProvider)
        : base(httpClientFactory, cookieProvider)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize a new terminal session
    /// Based on traced request: initSession() - Request 2
    /// </summary>
    public async Task<RestResponse<TerminalSessionResponse>> InitSessionAsync(long? nodeId = null, string? sessionId = null)
    {
        try
        {
            var request = new TerminalSessionRequest
            {
                NodeId = nodeId
            };

            var url = "api/terminal/init";
            if (!string.IsNullOrEmpty(sessionId))
            {
                url += $"?sessionId={sessionId}";
            }

            var response = await _httpClient.PostAsJsonAsync(url, request);
            var terminalSession = await response.Content.ReadFromJsonAsync<TerminalSessionResponse>();
            
            if (response.IsSuccessStatusCode && terminalSession != null && terminalSession.Success)
            {
                _logger.LogInformation("Terminal session initialized successfully: {SessionId}", terminalSession.SessionId);
                return RestResponse<TerminalSessionResponse>.Successful(terminalSession);
            }
            
            _logger.LogWarning("Failed to initialize terminal session");
            return new RestResponse<TerminalSessionResponse>
            {
                Success = false,
                Error = terminalSession?.Message ?? "Failed to initialize terminal session",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing terminal session");
            return new RestResponse<TerminalSessionResponse>
            {
                Success = false,
                Error = $"Terminal session initialization error: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Execute a command in the terminal session
    /// Based on traced request: execute(sessionId, command) - Request 1
    /// </summary>
    public async Task<RestResponse<TerminalExecuteResponse>> ExecuteAsync(string terminalSessionId, string command, string? authSessionId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(terminalSessionId))
                throw new ArgumentNullException(nameof(terminalSessionId));
            
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException(nameof(command));

            var request = new TerminalExecuteRequest
            {
                SessionId = terminalSessionId,
                Command = command
            };

            var url = "api/terminal/execute";
            if (!string.IsNullOrEmpty(authSessionId))
            {
                url += $"?sessionId={authSessionId}";
            }

            var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);
            var executeResult = await response.Content.ReadFromJsonAsync<TerminalExecuteResponse>(cancellationToken);
            
            if (response.IsSuccessStatusCode && executeResult != null && executeResult.Success)
            {
                _logger.LogInformation("Terminal command executed successfully: {Command}", command);
                return RestResponse<TerminalExecuteResponse>.Successful(executeResult);
            }
            
            _logger.LogWarning("Failed to execute terminal command: {Command}", command);
            return new RestResponse<TerminalExecuteResponse>
            {
                Success = false,
                Error = executeResult?.Message ?? "Failed to execute terminal command",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing terminal command: {Command}", command);
            return new RestResponse<TerminalExecuteResponse>
            {
                Success = false,
                Error = $"Terminal command execution error: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Execute a simple command using the convenience endpoint
    /// </summary>
    public async Task<RestResponse<TerminalExecuteResponse>> ExecuteAsync(string terminalSessionId, string command, string? authSessionId = null)
    {
        try
        {
            if (string.IsNullOrEmpty(terminalSessionId))
                throw new ArgumentNullException(nameof(terminalSessionId));
            
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException(nameof(command));

            var url = $"api/terminal/execute/{terminalSessionId}?command={Uri.EscapeDataString(command)}";
            if (!string.IsNullOrEmpty(authSessionId))
            {
                url += $"&sessionId={authSessionId}";
            }

            var response = await _httpClient.PostAsync(url, null);
            var executeResult = await response.Content.ReadFromJsonAsync<TerminalExecuteResponse>();
            
            if (response.IsSuccessStatusCode && executeResult != null && executeResult.Success)
            {
                _logger.LogInformation("Terminal command executed successfully: {Command}", command);
                return RestResponse<TerminalExecuteResponse>.Successful(executeResult);
            }

            _logger.LogWarning("Failed to execute terminal command: {Command}", command);
            return new RestResponse<TerminalExecuteResponse>
            {
                Success = false,
                Error = executeResult?.Message ?? "Failed to execute terminal command",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing terminal simple command: {Command}", command);
            return new RestResponse<TerminalExecuteResponse>
            {
                Success = false,
                Error = $"Terminal simple command execution error: {ex.Message}",
                Exception = ex
            };
        }
    }

    
}
