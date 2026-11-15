using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Tests;

// Test-only identity: the test sets the user in request headers. This handler never ships with the API.
public sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var user))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }
        List<Claim> claims = [new("sub", user.ToString())];
        claims.AddRange(Request.Headers["X-Test-Roles"].ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(role => new Claim(ClaimTypes.Role, role)));
        if (Request.Headers.TryGetValue("X-Test-Institution", out var institution))
        {
            claims.Add(new Claim("institution_id", institution.ToString()));
        }
        if (Request.Headers.TryGetValue("X-Test-Unit", out var unit))
        {
            claims.Add(new Claim("unit_id", unit.ToString()));
        }
        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}

public sealed record TestUser(string Id, int InstitutionId, int UnitId, params string[] Roles);

public static class TestUsers
{
    // Lakeside School (institution 1, unit "Library", approval limit 5,000 TRY)
    public static readonly TestUser Ada = new("ada", 1, 1, "InstitutionBuyer");
    public static readonly TestUser Grace = new("grace", 1, 1, "InstitutionApprover");
    public static readonly TestUser Linus = new("linus", 1, 1, "InstitutionBuyer", "InstitutionApprover");
    // Hillcrest University (institution 2)
    public static readonly TestUser Alan = new("alan", 2, 2, "InstitutionApprover");

    public static HttpClient CreateClientAs(this BookstoreApiFactory factory, TestUser user)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", user.Id);
        client.DefaultRequestHeaders.Add("X-Test-Roles", string.Join(",", user.Roles));
        client.DefaultRequestHeaders.Add("X-Test-Institution", user.InstitutionId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Unit", user.UnitId.ToString());
        return client;
    }
}
