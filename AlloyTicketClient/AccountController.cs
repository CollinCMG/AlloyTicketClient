using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

[Route("[controller]/[action]")]
public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = "/")
    {
        return Challenge(
            new AuthenticationProperties { RedirectUri = returnUrl ?? "/" },
            OpenIdConnectDefaults.AuthenticationScheme
        );
    }

    [HttpGet]
    public IActionResult Logout()
    {
        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            OpenIdConnectDefaults.AuthenticationScheme,
            "Cookies"
        );
    }
}
