using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace RSChatApp.Web.Controllers.Auth;

[Route("auth")]
public class AuthController : Controller
{
    [HttpGet("challenge")]
    public IActionResult Challenge(string returnUrl = null, bool popup = false)
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
}