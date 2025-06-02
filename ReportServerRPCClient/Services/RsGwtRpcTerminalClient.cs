using System.Text;
using Microsoft.Extensions.Logging;
using ReportServerPort.Contracts;
using ReportServerPort.Exceptions;
using ReportServerRPCClient.DTOs;
using ReportServerRPCClient.DTOs.Terminal;
using ReportServerRPCClient.Infrastructure;

namespace ReportServerRPCClient.Services;

public class RsGwtRpcTerminalClient : ReportServerGwtRpcClientBase
{
    private readonly ILogger<RsGwtRpcTerminalClient> _logger;

    public RsGwtRpcTerminalClient(ILoggerFactory loggerFactory, 
        HttpClient client, 
        CookieContainerProvider cookieProvider)
        : base(client, cookieProvider)
    {
        _logger = loggerFactory.CreateLogger<RsGwtRpcTerminalClient>();
    }
    
    public async Task<GwtRpcResponse> CloseSessionAsync(string sessionId)
    {
        try
        {
            _logger.LogInformation("Closing terminal session: {SessionId}", sessionId);

            var payload = BuildGwtRpcPayload(
                "net.datenwerke.rs.terminal.client.terminal.rpc.TerminalRpcService",
                "closeSession",
                sessionId
            );

            var content = new StringContent(payload, Encoding.UTF8, "text/x-gwt-rpc");
            var response = await _httpClient.PostAsync("/reportserver/terminal", content);

            response.EnsureSuccessStatusCode();
            var responseText = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Terminal session closed successfully: {SessionId}", sessionId);
            return ParseGwtResponse(responseText);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to close terminal session: {SessionId}", sessionId);
            return GwtRpcResponse.Fail(ex.Message, ex);
        }
    }

    public async Task<GwtRpcResponse<TerminalSessionInfoDto>> InitSessionAsync(AbstractNodeDto node = null, Dto2PosoMapper mapper = null)
    {
        try
        {
            _logger.LogInformation("Initializing terminal session for node: {NodeId}", node?.Id);

            var payload = BuildGwtRpcPayload(
                "net.datenwerke.rs.terminal.client.terminal.rpc.TerminalRpcService",
                "initSession",
                node,
                mapper
            );

            var content = new StringContent(payload, Encoding.UTF8, "text/x-gwt-rpc");
            var response = await _httpClient.PostAsync("/reportserver/terminal", content);

            response.EnsureSuccessStatusCode();
            var responseText = await response.Content.ReadAsStringAsync();
            
            if (responseText.StartsWith("//EX"))
            {
                var errorMessage = ExtractErrorMessage(responseText);
                return new GwtRpcResponse<TerminalSessionInfoDto>
                {
                    Success = false,
                    Error = errorMessage,
                    Exception = new ServerCallFailedException(errorMessage)
                };
            }

            var sessionData = ParseGwtResponse<Dictionary<string, string>>(responseText);
            var sessionInfo = new TerminalSessionInfoDto
            {
                SessionId = sessionData.Result.GetValueOrDefault("sessionId"),
                Prompt = sessionData.Result.GetValueOrDefault("prompt", "rs> "),
                WorkingDirectory = sessionData.Result.GetValueOrDefault("workingDirectory", "/"),
                Environment = sessionData.Result
            };

            _logger.LogInformation("Terminal session initialized: {SessionId}", sessionInfo.SessionId);
            return new GwtRpcResponse<TerminalSessionInfoDto>
            {
                Success = true,
                Result = sessionInfo,
                Message = responseText
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize terminal session");
            return new GwtRpcResponse<TerminalSessionInfoDto>
            {
                Success = false,
                Error = ex.Message,
                Exception = ex
            };
        }
    }

    public async Task<GwtRpcResponse<AutocompleteResultDto>> AutocompleteAsync(string sessionId, string command, int cursorPosition, bool forceResult = false)
    {
        try
        {
            _logger.LogDebug("Autocomplete request - Session: {SessionId}, Command: {Command}, Position: {Position}", 
                sessionId, command, cursorPosition);

            var payload = BuildGwtRpcPayload(
                "net.datenwerke.rs.terminal.client.terminal.rpc.TerminalRpcService",
                "autocomplete",
                sessionId,
                command,
                cursorPosition,
                forceResult
            );

            var content = new StringContent(payload, Encoding.UTF8, "text/x-gwt-rpc");
            var response = await _httpClient.PostAsync("/reportserver/terminal", content);

            response.EnsureSuccessStatusCode();
            var responseText = await response.Content.ReadAsStringAsync();
            return ParseGwtResponse<AutocompleteResultDto>(responseText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Autocomplete failed - Session: {SessionId}", sessionId);
            return new GwtRpcResponse<AutocompleteResultDto>
            {
                Success = false,
                Error = ex.Message,
                Exception = ex
            };
        }
    }

    public async Task<GwtRpcResponse<CommandResultDto>> ExecuteAsync(string sessionId, string command)
    {
        try
        {
            _logger.LogInformation("Executing command - Session: {SessionId}, Command: {Command}", sessionId, command);
            var payload = BuildGwtRpcPayload(
                "net.datenwerke.rs.terminal.client.terminal.rpc.TerminalRpcService",
                "execute",
                sessionId,
                command
            );

            var content = new StringContent(payload, Encoding.UTF8, "text/x-gwt-rpc");
            var response = await _httpClient.PostAsync("/reportserver/terminal", content);

            response.EnsureSuccessStatusCode();
            var responseText = await response.Content.ReadAsStringAsync();

            var result = ParseGwtResponse<CommandResultDto>(responseText);
            _logger.LogDebug("Command executed - Result type: {Type}", command);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command execution failed - Session: {SessionId}, Command: {Command}", sessionId, command);
            return new GwtRpcResponse<CommandResultDto>
            {
                Success = false,
                Error = ex.Message,
                Exception = ex
            };
        }
    }

    public async Task<GwtRpcResponse<CommandResultDto>> CtrlCPressedAsync(string sessionId)
    {
        try
        {
            _logger.LogInformation("Sending Ctrl+C interrupt - Session: {SessionId}", sessionId);

            var payload = BuildGwtRpcPayload(
                "net.datenwerke.rs.terminal.client.terminal.rpc.TerminalRpcService",
                "ctrlCPressed",
                sessionId
            );

            var content = new StringContent(payload, Encoding.UTF8, "text/x-gwt-rpc");
            var response = await _httpClient.PostAsync("/reportserver/terminal", content);

            response.EnsureSuccessStatusCode();
            var responseText = await response.Content.ReadAsStringAsync();
            return ParseGwtResponse<CommandResultDto>(responseText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ctrl+C failed - Session: {SessionId}", sessionId);
            return new GwtRpcResponse<CommandResultDto>
            {
                Success = false,
                Error = ex.Message,
                Exception = ex
            };
        }
    }

}