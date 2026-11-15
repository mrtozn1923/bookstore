using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Bookstore.Api.Tests;

// Signs tokens the way Bookstore.Admin does, with a key that exists only in the tests.
public static class TestTokens
{
    public const string SigningKey = "test-only-signing-key-for-api-tests-not-real";

    public static string For(string name, params string[] roles) => ForKey(SigningKey, name, roles);

    public static string ForKey(string signingKey, string name, params string[] roles)
    {
        List<Claim> claims = [new("sub", name), new("name", name), .. roles.Select(r => new Claim("role", r))];
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = "bookstore-admin",
            Audience = "bookstore-api",
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256)
        });
    }
}
