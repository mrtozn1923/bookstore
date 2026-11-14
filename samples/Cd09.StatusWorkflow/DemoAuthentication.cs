using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Bookstore.Institutional.Workflow;

// Demo only: "X-Demo-User: ada" signs the request in as a known sample user. Use real authentication in an app.
public sealed class DemoAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Demo";

    public static readonly Dictionary<string, string[]> Users = new()
    {
        ["ada"] = ["InstitutionBuyer"],
        ["grace"] = ["InstitutionApprover"],
        ["alan"] = ["InstitutionApprover"]
    };

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? name = Request.Headers["X-Demo-User"];
        if (name is null || !Users.TryGetValue(name, out string[]? roles))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }
        Claim[] claims = [new(ClaimTypes.Name, name), .. roles.Select(r => new Claim(ClaimTypes.Role, r))];
        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
