using System.Security.Claims;
using Bookstore.Admin.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace Bookstore.Admin.Tests;

// Signed-in staff members for component tests, shaped like the principal ASP.NET Core Identity creates.
public static class TestStaff
{
    public const string SigningKey = "test-only-signing-key-for-admin-tests-not-real";

    public static ClaimsPrincipal User(string email, params string[] roles) => new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, $"id-{email}"),
            new Claim(ClaimTypes.Name, email),
            .. roles.Select(r => new Claim(ClaimTypes.Role, r))
        ],
        authenticationType: "Identity.Application"));

    public static PanelTokenIssuer Tokens() => new(Options.Create(new PanelTokenOptions
    {
        Issuer = "bookstore-admin",
        Audience = "bookstore-api",
        SigningKey = SigningKey
    }), TimeProvider.System);
}

public sealed class FixedAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
}
