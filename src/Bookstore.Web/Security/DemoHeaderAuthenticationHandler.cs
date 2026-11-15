using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Bookstore.Web.Security;

public sealed class DemoHeaderOptions : AuthenticationSchemeOptions
{
    public bool Enabled { get; set; }
}

// Demo only: "X-Demo-Role: StoreStaff" signs the request in. Real sign-in is cookie authentication
// (Authentication and Authorization series). Turned on with Demo:HeaderAuth=true (appsettings.Development.json).
public sealed class DemoHeaderAuthenticationHandler(
    IOptionsMonitor<DemoHeaderOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<DemoHeaderOptions>(options, logger, encoder)
{
    public const string SchemeName = "DemoHeader";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? role = Request.Headers["X-Demo-Role"];
        if (!Options.Enabled || string.IsNullOrEmpty(role))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        ClaimsIdentity identity = new([new Claim(ClaimTypes.Name, "ada"), new Claim(ClaimTypes.Role, role)], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
