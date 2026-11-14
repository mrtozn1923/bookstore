using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Identity;

public sealed record DemoUser(string Name, string Role, string? InstitutionId = null, string? UnitId = null);

// Stand-in for real JWT bearer authentication (covered in the API Security series):
// "Authorization: Bearer ada" signs in as the demo user "ada".
public static class DemoUsers
{
    public static readonly IReadOnlyDictionary<string, DemoUser> All = new Dictionary<string, DemoUser>
    {
        ["ada"] = new("ada", "Customer"),
        ["nevin"] = new("nevin", "InstitutionBuyer", "lakeside-school", "main-campus"),
        ["alan"] = new("alan", "InstitutionBuyer", "lakeside-school", "east-campus"),
        ["linus"] = new("linus", "InstitutionApprover", "lakeside-school"),
        ["grace"] = new("grace", "InstitutionBuyer", "city-library", "central"),
        ["ayse"] = new("ayse", "StoreStaff"),
    };

    public static DemoUser Current(this ClaimsPrincipal principal) =>
        All[principal.Identity?.Name ?? throw new InvalidOperationException("Not authenticated.")];
}

public sealed class DemoBearerHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DemoBearer";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? header = Request.Headers.Authorization;
        if (header is null || !header.StartsWith("Bearer ", StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string name = header["Bearer ".Length..].Trim();
        if (!DemoUsers.All.TryGetValue(name, out DemoUser? user))
        {
            return Task.FromResult(AuthenticateResult.Fail("Unknown demo user."));
        }

        Claim[] claims = [new(ClaimTypes.Name, user.Name), new(ClaimTypes.Role, user.Role)];
        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
