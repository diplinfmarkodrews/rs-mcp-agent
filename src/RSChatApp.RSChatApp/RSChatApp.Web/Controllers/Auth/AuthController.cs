using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using RSChatApp.Infrastructure.Identity.Services;
using RsMcpServer.Identity.Models.Requests;

namespace RSChatApp.Web.Controllers.Auth;
//Only for keycloak challenge redirection, should go into mcp server
[Route("auth")]
public class AuthController : Controller
{
    private readonly ILegacyAuthenticationService _legacyAuthenticationService;

    public AuthController(ILegacyAuthenticationService legacyAuthenticationService)
    {
        _legacyAuthenticationService = legacyAuthenticationService;
    }
    [HttpGet("challenge")]
    public IActionResult Challenge(string? returnUrl = null, bool popup = false)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = popup ? "/auth/popup-auth-success" : returnUrl ?? "/"
        };
        
        if (popup)
        {
            properties.Items["popup"] = "true";
        }
        
        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }
    
    [HttpGet("popup-auth-success")]
    public IActionResult PopupAuthSuccess()
    {
        return View();
    }
    [HttpPost("legacy-login")]
    public async Task<IActionResult> LegacyLogin([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var authResult = await _legacyAuthenticationService.AuthenticateAsync(
            request.Username, 
            request.Password, 
            cancellationToken);
       
        return Ok(new { Success = authResult.IsAuthenticated, Error = authResult.Error });
    }

  
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _legacyAuthenticationService.LogoutAsync(cancellationToken);
        return Ok();
    }
}