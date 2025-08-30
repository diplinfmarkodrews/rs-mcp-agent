using AutoMapper;
using Microsoft.Extensions.Logging;
using ReportServer.Abstraction;
using ReportServer.Abstraction.Contracts;
using ReportServer.Abstraction.Contracts.Authentication;
using ReportServer.Abstraction.Contracts.Terminal;
using ReportServer.RestClient.DTOs.Terminal;
using ReportServer.RestClient.Infrastructure;


namespace ReportServer.RestClient.Services;

public class RsRestClient : RsRestClientBase, IReportServerClient
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly RsRestAuthenticationClient _authenticationClient;
    // private readonly RsRestFileServerClient _fileServerClient;
    // private readonly RsRestRemoteServerClient _remoteServerClient;
    private readonly RsRestTerminalClient _terminalClient;

    public RsRestClient(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, 
        CookieContainerProvider cookieProvider, 
        IMapper mapper) 
        : base(httpClientFactory, cookieProvider)
    {
        _authenticationClient = new RsRestAuthenticationClient(loggerFactory.CreateLogger<RsRestAuthenticationClient>(), httpClientFactory, cookieProvider);
        _terminalClient = new RsRestTerminalClient(loggerFactory.CreateLogger<RsRestTerminalClient>(), httpClientFactory, cookieProvider);
        _logger = loggerFactory.CreateLogger<RsRestClient>();
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

    public async Task<Result<string>> LogoutAsync()
    {
        try
        {
            var rsResponse = await _authenticationClient.LogoutAsync();
            if (rsResponse.Success)
            {
                return new Result<string>(rsResponse.Result);
            }

            return new Result<string>(rsResponse.Exception);
        }
        catch(Exception exception)
        {
            return new Result<string>(exception);
        }
    }
    #endregion
    #region FileServer Operations
    // public async Task<Result<string>> LoadFileTreeAsync()
    // {
    //     try
    //     {
    //         var response = await _fileServerClient.LoadFileTreeAsync();
    //         return new Result<string>(response)
    //         {
    //             IsSuccess = true
    //         };
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Failed to load file tree");
    //         return new Result<string>(ex);
    //     }
    // }
    #endregion
    #region Terminal Operations
    /// <summary>
    /// Terminal
    /// </summary>
    public async Task<Result> CloseSessionAsync(string sessionId)
    {
        // var response = await _terminalClient.CloseSessionAsync(sessionId);
        // if (response.Success)
        // {
        //     return Result.Success("Session closed successfully");
        // }
        // return Result.Fail(response.Error, response.Exception);
        throw new NotImplementedException();
    }

    public async Task<Result<TerminalSessionInfo>> InitSessionAsync(AbstractNode node = null, Dictionary<string, string> mapper = null)
    {                
        var response = await _terminalClient.InitSessionAsync();
        if (response.Success)
        {
            return new Result<TerminalSessionInfo>(
                _mapper.Map<TerminalSessionInfo>(response.Result));
        }
        return new Result<TerminalSessionInfo>(response.Exception);
    }

    public async Task<Result<CommandResult>> ExecuteAsync(string sessionId, string command, CancellationToken cancellationToken = default)
    {
        var response = await _terminalClient.ExecuteAsync(sessionId, command, cancellationToken: cancellationToken);
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

