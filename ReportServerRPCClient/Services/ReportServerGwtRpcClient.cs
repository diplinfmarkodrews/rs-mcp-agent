using AutoMapper;
using Microsoft.Extensions.Logging;
using ReportServerPort;
using ReportServerPort.Authentication.Contracts;
using ReportServerPort.Contracts;
using ReportServerPort.Contracts.Terminal;
using ReportServerPort.Exceptions;
using ReportServerRPCClient.DTOs.Terminal;
using ReportServerRPCClient.Infrastructure;


namespace ReportServerRPCClient.Services;

public class ReportServerGwtRpcClient : ReportServerGwtRpcClientBase, IReportServerClient
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly RsGwtRpcAuthenticationClient _authenticationClient;
    private readonly RsGwtRpcFileServerClient _fileServerClient;
    private readonly RsGwtRpcRemoteServerClient _remoteServerClient;
    private readonly RsGwtRpcTerminalClient _terminalClient;

    public ReportServerGwtRpcClient(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, 
        CookieContainerProvider cookieProvider, 
        IMapper mapper) 
        : base(httpClientFactory.CreateClient("ReportServerGwtRpcClient"), cookieProvider)
    {
        _authenticationClient = new RsGwtRpcAuthenticationClient(_httpClient, cookieProvider);
        _fileServerClient = new RsGwtRpcFileServerClient(_httpClient, cookieProvider);
        _remoteServerClient = new RsGwtRpcRemoteServerClient(_httpClient, cookieProvider);
        _terminalClient = new RsGwtRpcTerminalClient(loggerFactory, _httpClient, cookieProvider);
        _mapper = mapper;
        _logger = loggerFactory.CreateLogger<ReportServerGwtRpcClient>();
    }
    #region Authentication
    
    public async Task<Result<AuthenticationResult>> AuthenticateAsync(string username, string password)
    {
        try
        {
            var rsResponse = await _authenticationClient.AuthenticateAsync(username, password);
            if (rsResponse.Success)
            {
                return new Result<AuthenticationResult>(
                    new AuthenticationResult
                    {
                        SessionId = rsResponse.Result.SessionId,
                        User = _mapper.Map<User>(rsResponse.Result.User)
                    });
            }

            return new Result<AuthenticationResult>(rsResponse.Exception);
        }
        catch(Exception exception)
        {
            return new Result<AuthenticationResult>(exception);
        }
    }
    #endregion
    #region FileServer Operations
    public async Task<Result<string>> LoadFileTreeAsync()
    {
        try
        {
            var response = await _fileServerClient.LoadFileTreeAsync();
            return new Result<string>(response)
            {
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load file tree");
            return new Result<string>(ex);
        }
    }
    #endregion
    #region Terminal Operations
    /// <summary>
    /// Terminal
    /// </summary>
    public async Task<Result> CloseSessionAsync(string sessionId)
    {
        var response = await _terminalClient.CloseSessionAsync(sessionId);
        if (response.Success)
        {
            return Result.Success("Session closed successfully");
        }
        return Result.Fail(response.Error, response.Exception);
    }

    public async Task<Result<TerminalSessionInfo>> InitSessionAsync(AbstractNode node = null, Dictionary<string, string> mapper = null)
    {
        var abstractNodeDto = node is not null ?
            _mapper.Map<AbstractNodeDto>(node) :
            null;
        var mappings = mapper is not null ?
            new Dto2PosoMapper
            {
                Mappings = mapper
            }:
            null;
        
        var response = await _terminalClient.InitSessionAsync(abstractNodeDto, mappings);
        if (response.Success)
        {
            return new Result<TerminalSessionInfo>(
                _mapper.Map<TerminalSessionInfo>(response.Result));
        }
        return new Result<TerminalSessionInfo>(response.Exception);
    }

    public async Task<Result<CommandResult>> ExecuteAsync(string sessionId, string command, CancellationToken cancellationToken = default)
    {
        var response = await _terminalClient.ExecuteAsync(sessionId, command, cancellationToken);
        if (response.Success)
        {
            return new Result<CommandResult>(
                _mapper.Map<CommandResult>(response.Result));
        }
        return new Result<CommandResult>(response.Error);
    }

    public async Task<Result<CommandResult>> CtrlCPressedAsync(string sessionId)
    {
        throw new NotImplementedException();
    }
    #endregion
}

