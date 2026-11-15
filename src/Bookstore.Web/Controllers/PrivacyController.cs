using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web.Controllers;

[Route("privacy")]
public sealed class PrivacyController : Controller
{
    [HttpPost("consent")]
    [ValidateAntiForgeryToken]
    public IActionResult Consent(string? returnUrl)
    {
        HttpContext.Features.Get<ITrackingConsentFeature>()?.GrantConsent();
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }

    [HttpPost("withdraw")]
    [ValidateAntiForgeryToken]
    public IActionResult Withdraw(string? returnUrl)
    {
        HttpContext.Features.Get<ITrackingConsentFeature>()?.WithdrawConsent();
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }
}
