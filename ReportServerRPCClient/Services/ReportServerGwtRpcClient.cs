using System.Net;
using System.Text;
using AutoMapper;
using Newtonsoft.Json;
using ReportServerPort;
using ReportServerPort.Contracts;
using ReportServerRPCClient.DTOs.Authentication;
using ReportServerRPCClient.DTOs.FileServer;
using ReportServerRPCClient.DTOs.RemoteServer;
using ReportServerRPCClient.Infrastructure;

namespace ReportServerRPCClient.Services;

public class ReportServerGwtRpcClient : ReportServerGwtRpcClientBase, IReportServerClient
{
    
    private readonly RsGwtRpcAuthenticationClient _authenticationClient;
    private readonly RsGwtRpcFileServerClient _fileServerClient;
    private readonly RsGwtRpcRemoteServerClient _remoteServerClient;
    private readonly IMapper _mapper;

    public ReportServerGwtRpcClient(IHttpClientFactory httpClientFactory, 
        CookieContainerProvider cookieProvider, 
        IMapper mapper) 
        : base(httpClientFactory.CreateClient("ReportServerGwtRpcClient"), cookieProvider)
    {
        _authenticationClient = new RsGwtRpcAuthenticationClient(_httpClient, cookieProvider);
        _fileServerClient = new RsGwtRpcFileServerClient(_httpClient, cookieProvider);
        _remoteServerClient = new RsGwtRpcRemoteServerClient(_httpClient, cookieProvider);
        _mapper = mapper;
    }
    
    public async Task<Result<AuthenticationResult>> AuthenticateAsync(string username, string password)
    {
        try
        {
            var rsResponse = await _authenticationClient.AuthenticateAsync(username, password);
            if (rsResponse.Success)
            {
                return new Result<AuthenticationResult>(new AuthenticationResult
                {
                    SessionId = rsResponse.SessionId,
                    User = _mapper.Map<User>(rsResponse.User)
                })
                {
                    IsSuccess = true
                };
            }

            return new Result<AuthenticationResult>(rsResponse.ErrorMessage);
        }
        catch(Exception exception)
        {
            return new Result<AuthenticationResult>(exception.Message)
            {
                IsSuccess = false,
                Error = new SerializableException(exception),
                Message = exception.Message
            };
        }
    }
        }

