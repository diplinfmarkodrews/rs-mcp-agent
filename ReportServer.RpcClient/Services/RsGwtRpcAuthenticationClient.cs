using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ReportServer.Abstraction.Contracts;
using ReportServer.Abstraction.Exceptions;
using ReportServer.RpcClient.DTOs;
using ReportServer.RpcClient.DTOs.Authentication;
using ReportServer.RpcClient.Infrastructure;

namespace ReportServer.RpcClient.Services;

public class RsGwtRpcAuthenticationClient : ReportServerGwtRpcClientBase
{
    private readonly ILogger<RsGwtRpcAuthenticationClient> _logger;

    // Service hashes from traces
    private const string LOGIN_SERVICE_HASH = "DFEDD0FBBBBBE222F217D04F50A95F56";
    private const string SECURITY_SERVICE_HASH = "1D8BB90B3362E3AB16AD5D9EC9568CE7";
    private const string CHALLENGE_SERVICE_HASH = "B6F10AD9852902823F606D81A985ACC7";
    
    // Constructor
    public RsGwtRpcAuthenticationClient(ILogger<RsGwtRpcAuthenticationClient> logger, HttpClient httpClient, CookieContainerProvider cookieProvider)
        : base(httpClient, cookieProvider)
    {
        if (_httpClient.BaseAddress is null)
            throw new InvalidOperationException("BaseAddress not set in HTTP client.");
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user with username/password - Based on request trace #4
    /// </summary>
    public async Task<GwtRpcResponse<AuthenticationResultDto>> AuthenticateAsync(string username, string password)
    {
        try
        {
            // Build payload exactly matching the actual working trace
            //$"7|0|7|{_moduleBaseUrl}|{LOGIN_SERVICE_HASH}|net.datenwerke.rs.authenticator.client.login.rpc.LoginHandler|authenticate|[Lnet.datenwerke.security.client.login.AuthToken;/1508143471|net.datenwerke.rs.authenticator.client.login.dto.UserPasswordAuthToken/1647979090|{username}|1|2|3|4|1|5|5|1|6|7|7|"
            var payload = $"7|0|8|{_moduleBaseUrl}|{LOGIN_SERVICE_HASH}|net.datenwerke.rs.authenticator.client.login.rpc.LoginHandler|authenticate|[Lnet.datenwerke.security.client.login.AuthToken;/1508143471|net.datenwerke.rs.authenticator.client.login.dto.UserPasswordAuthToken/1647979090|{password}|{username}|1|2|3|4|1|5|5|1|6|7|8|";
            
            var response = await PostGwtRpcAsync("login", payload, true);
            var parsedResult = ParseAuthenticationResponse(response);
            if (parsedResult != null)
                return GwtRpcResponse<AuthenticationResultDto>.Successful(parsedResult);
            
            if (TryParseException(response, out var responseException))
                return GwtRpcResponse<AuthenticationResultDto>.Fail(responseException);
            
            _logger.LogError("Authentication failed, response could not be read: {Response}", response);
            return GwtRpcResponse<AuthenticationResultDto>.Fail(new ServerCallFailedException("Authentication failed, response could not be read"));
        }
        catch(Exception ex)
        {
            return GwtRpcResponse<AuthenticationResultDto>.Fail(ex);
        }
    }

    /// <summary>
    /// Check if current session is authenticated - Based on request trace #1
    /// </summary>
    public async Task<GwtRpcResponse<AuthenticationResultDto>> IsAuthenticatedAsync()
    {
        // Build payload based on trace #1:
        // 7|0|4|http://localhost:8090/reportserver/|DFEDD0FBBBBBE222F217D04F50A95F56|net.datenwerke.rs.authenticator.client.login.rpc.LoginHandler|isAuthenticated|1|2|3|4|0|
        var payload = $"7|0|4|{_moduleBaseUrl}|{LOGIN_SERVICE_HASH}|net.datenwerke.rs.authenticator.client.login.rpc.LoginHandler|isAuthenticated|1|2|3|4|0|";
        try
        {

            var response = await PostGwtRpcAsync("login", payload, extractSessionCookie: true);
            var parsedResult = ParseSessionCheckResponse(response);

            if (parsedResult != null)
                return GwtRpcResponse<AuthenticationResultDto>.Successful(parsedResult);
            
            if (TryParseException(response, out var responseException))
                return GwtRpcResponse<AuthenticationResultDto>.Fail(responseException);
            
            _logger.LogError("IsAuthenticatedAsync failed parsing response: {Response}", response);
            return GwtRpcResponse<AuthenticationResultDto>.Fail(new ServerCallFailedException("Failed to parse isAuthenticated response"));
        }
        catch (Exception ex)
        {
            return GwtRpcResponse<AuthenticationResultDto>.Fail(ex);
        }
    }

    /// <summary>
    /// Get HMAC passphrase for password encryption - Based on request trace #3
    /// </summary>
    public async Task<GwtRpcResponse<string>> GetHmacPassphraseAsync()
    {
        // Build payload based on trace #3:
        // 7|0|4|http://localhost:8090/reportserver/|B6F10AD9852902823F606D81A985ACC7|net.datenwerke.rs.authenticator.cr.client.ChallengeResponseRpcService|getHmacPassphrase|1|2|3|4|0|
        var payload = $"7|0|4|{_moduleBaseUrl}|{CHALLENGE_SERVICE_HASH}|net.datenwerke.rs.authenticator.cr.client.ChallengeResponseRpcService|getHmacPassphrase|1|2|3|4|0|";
        
        var response = await PostGwtRpcAsync("security_challengeresponse", payload);
        var passphrase = ParseChallengeResponse(response);
        
        if (!string.IsNullOrEmpty(passphrase))
            return GwtRpcResponse<string>.Successful(passphrase);
        
        if (TryParseException(response, out var responseException))
            return GwtRpcResponse<string>.Fail(responseException);
        
        _logger.LogError("GetHmacPassphraseAsync failed parsing response: {Response}", response);
        return GwtRpcResponse<string>.Fail(new ServerCallFailedException("Failed to get HMAC passphrase"));
    }

    /// <summary>
    /// Load generic security rights - Based on request trace #2
    /// </summary>
    public async Task<GwtRpcResponse<Dictionary<string, object>>> LoadGenericRightsAsync()
    {
        // Build payload based on trace #2 (simplified version):
        // This is a complex payload with many type parameters, using simplified version
        var payload = $"7|0|31|{_moduleBaseUrl}|{SECURITY_SERVICE_HASH}|net.datenwerke.security.ext.client.security.rpc.SecurityRpcService|loadGenericRights|java.util.Collection|java.util.HashSet/3273092938|net.datenwerke.rs.core.client.genrights.AccessRsGenericTargetIdentifier/1217528734|net.datenwerke.gf.client.administration.security.AdminGenericTargetIdentifier/745751986|net.datenwerke.rs.dashboard.client.dashboard.security.DashboardViewGenericTargetIdentifier/2641288325|net.datenwerke.rs.dashboard.client.dashboard.security.DashboardAdminGenericTargetIdentifier/138113631|net.datenwerke.security.ext.client.security.ui.genericview.targets.GenericSecurityTargetAdminViewGenericTargetIdentifier/2555008146|net.datenwerke.rs.terminal.client.terminal.security.TerminalGenericTargetIdentifier/3565892166|net.datenwerke.rs.core.client.datasourcemanager.security.DatasourceManagerGenericTargetIdentifier/4279957352|net.datenwerke.rs.transport.client.transport.security.TransportGenericTargetIdentifier/484713213|net.datenwerke.rs.transport.client.transport.security.TransportManagementGenericTargetIdentifier/1443445958|net.datenwerke.rs.adminutils.client.systemconsole.security.SystemConsoleGenericTargetIdentifier/2950244943|net.datenwerke.rs.remoteserver.client.remoteservermanager.security.RemoteServerManagerGenericTargetIdentifier/196606530|net.datenwerke.rs.core.client.datasinkmanager.security.DatasinkManagerGenericTargetIdentifier/1228933433|net.datenwerke.rs.fileserver.client.fileserver.security.FileServerManagerGenericTargetIdentifier/3507781659|net.datenwerke.rs.eximport.client.eximport.security.ExportGenericTargetIdentifier/1153950009|net.datenwerke.rs.eximport.client.eximport.security.ImportGenericTargetIdentifier/4134686308|net.datenwerke.rs.globalconstants.client.globalconstants.security.GlobalConstantsGenericTargetIdentifier/1920137750|net.datenwerke.rs.license.client.security.LicenseGenericTargetIdentifier/3867290533|net.datenwerke.rs.teamspace.client.teamspace.security.TeamSpaceGenericTargetIdentifier/1646070704|net.datenwerke.rs.core.client.reportmanager.security.ReportManagerGenericTargetIdentifier/1706906031|net.datenwerke.rs.adminutils.client.suuser.security.SuGenericTargetIdentifier/4065258493|net.datenwerke.rs.remoteaccess.client.sftp.genrights.SftpGenericTargetIdentifier/814877659|net.datenwerke.security.ext.client.usermanager.security.UserManagerAdminViewGenericTargetIdentifier/3165328799|net.datenwerke.rs.uservariables.client.uservariables.genrights.UserVariableAdminViewGenericTargetIdentifier/771125979|net.datenwerke.rs.scheduler.client.scheduler.security.SchedulingAdminViewGenericTargetIdentifier/1205179919|net.datenwerke.rs.scheduler.client.scheduler.security.SchedulingBasicGenericTargetIdentifier/1162125790|1|2|3|4|1|5|6|25|7|8|9|10|11|12|13|14|15|16|17|18|19|20|21|22|23|24|25|26|27|28|29|30|31|";
        var response = await PostGwtRpcAsync("security_security", payload);
        var rights = ParseSecurityRightsResponse(response);
        if (TryParseException(response, out var responseException))
            return GwtRpcResponse<Dictionary<string, object>>.Fail(responseException);
        
        return GwtRpcResponse<Dictionary<string, object>>.Successful(rights);
    }

    #region Response Parsing Methods

    private AuthenticationResultDto? ParseAuthenticationResponse(string gwtResponse)
    {
        if (gwtResponse.StartsWith("//OK"))
        {
            var sessionId = ExtractSessionFromCookies();
            var userData = ParseUserDataFromAuthResponse(gwtResponse);

            return new AuthenticationResultDto
            {
                IsAuthenticated = true,
                SessionId = sessionId ?? string.Empty,
                User = userData
            };
        }
        return null;
    }

    private AuthenticationResultDto? ParseSessionCheckResponse(string gwtResponse)
    {
        
        if (gwtResponse.StartsWith("//OK"))
        {
            var sessionId = ExtractSessionFromCookies();
            var userData = ParseUserDataFromSessionCheck(gwtResponse);

            return new AuthenticationResultDto
            {
                IsAuthenticated = true,
                SessionId = sessionId ?? string.Empty,
                User = userData
            };
        }

        return null;
    }

    private string? ParseChallengeResponse(string gwtResponse)
    {
        // Parse response: //OK[1,["This is the Passphrase used to compute the HMAC key for reportServer passwords."],0,7]
        if (!gwtResponse.StartsWith("//OK")) return null;
        
        var match = Regex.Match(gwtResponse, @"\[""([^""]+)""\]");
        return match.Success ? match.Groups[1].Value : null;
    }

    private Dictionary<string, object> ParseSecurityRightsResponse(string gwtResponse)
    {
        // This is a complex GWT response containing security rights information
        // For now, return a simplified structure - full implementation would need comprehensive GWT parsing
        var rights = new Dictionary<string, object>();
        
        if (gwtResponse.StartsWith("//OK"))
        {
            rights["success"] = true;
            rights["hasRights"] = true;
            // TODO: Parse detailed rights from complex GWT structure
        }
        else
        {
            rights["success"] = false;
        }
        
        return rights;
    }

    private UserDto ParseUserDataFromAuthResponse(string gwtResponse)
    {
        // Parse authentication response (trace #4) which contains AuthenticateResultDto
        // String table: ["net.datenwerke.security.client.login.AuthenticateResultDto/1984250979","java.util.ArrayList/4159755760","net.datenwerke.security.client.usermanager.dto.decorator.UserDtoDec/3663459877","nobody@datenwerke.net","root",...]
        var result = ParseUserDataFromStringTable(gwtResponse, 4) ?? new UserDto { Username = "unknown", Active = true, Properties = new Dictionary<string, string>(), Groups = new List<GroupDto>() }; // Username at index 4 for auth response
        return result;
    }

    private UserDto ParseUserDataFromSessionCheck(string gwtResponse)
    {
        // Parse session check response (trace #1) which contains UserDtoDec directly
        // String table: ["net.datenwerke.security.client.usermanager.dto.decorator.UserDtoDec/3663459877","nobody@datenwerke.net","root",...]
        return ParseUserDataFromStringTable(gwtResponse, 2) ?? new UserDto { Username = "unknown", Active = true, Properties = new Dictionary<string, string>(), Groups = new List<GroupDto>() }; // Username at index 2 for session check
    }

    private UserDto? ParseUserDataFromStringTable(string gwtResponse, int usernameIndex)
    {
        try
        {
            // The GWT response has the string table as a JSON array at the very end before metadata
            // Format: ...,["string1","string2",...],0,7]
            // We need to find the last opening bracket that starts a quoted-string array
            var startIdx = gwtResponse.LastIndexOf(",[\"");
            if (startIdx == -1) return null;
            
            // Find the matching closing bracket
            var endIdx = gwtResponse.IndexOf("]", startIdx + 1);
            if (endIdx == -1) return null;
            
            // Extract just the array content without the brackets
            var stringTableWithBrackets = gwtResponse.Substring(startIdx + 1, endIdx - startIdx);
            var stringTable = stringTableWithBrackets.Substring(1, stringTableWithBrackets.Length - 2); // Remove [ and ]
            
            // Split by "," pattern (comma followed by quote)
            var parts = Regex.Split(stringTable, "\",\"");
            
            // Remove leading/trailing quotes from first and last elements
            if (parts.Length > 0)
            {
                parts[0] = parts[0].TrimStart('"');
                parts[parts.Length - 1] = parts[parts.Length - 1].TrimEnd('"');
            }
            
            if (parts.Length > usernameIndex && parts.Length > usernameIndex - 1)
            {
                var username = parts[usernameIndex];
                var email = parts[usernameIndex - 1];                

                return new UserDto
                {
                    Username = username,
                    Email = email ?? string.Empty,
                    Active = true,
                    Properties = new Dictionary<string, string>(),
                    Groups = new List<GroupDto>()
                };
            }
        }
        catch
        {
            
        }

        return new UserDto
        {
            Username = "unknown",
            Active = true,
            Properties = new Dictionary<string, string>(),
            Groups = new List<GroupDto>()
        };
    }

    private string? ExtractSessionFromCookies()
    {
        if (_httpClient.BaseAddress == null) return null;
        
        var cookies = _cookieContainer.GetCookies(_httpClient.BaseAddress);
        var sessionCookie = cookies[CookieSessionId];
        return sessionCookie?.Value;
    }

    public Task<Result<string>> LogoutAsync()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Compute HMAC hash for password using the server's passphrase
    /// </summary>
    private string ComputeHmacHash(string password, string passphrase)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA1(System.Text.Encoding.UTF8.GetBytes(passphrase));
        var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashBytes);
    }

    #endregion
}