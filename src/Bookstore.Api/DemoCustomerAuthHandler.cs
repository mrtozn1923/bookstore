using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Bookstore.Api;

// Local demo only: the customer id comes from a header. Real sign-in belongs to ASP.NET Core Identity.
public sealed class DemoCustomerAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DemoCustomer";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!int.TryParse(Request.Headers["X-Demo-Customer-Id"], out int id))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
