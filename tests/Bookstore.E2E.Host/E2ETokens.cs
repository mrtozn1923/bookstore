using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Bookstore.E2E.Host;

public sealed record TokenRequest(int CustomerId, string[]? Roles);

// Production'daki identity provider'ın yerini tutar: her koşuda yeni bir anahtarla token imzalar
public sealed class E2ETokenIssuer
{
    public const string Issuer = "bookstore-e2e";

    public SymmetricSecurityKey SigningKey { get; } = new(RandomNumberGenerator.GetBytes(32));

    public string Issue(TokenRequest request)
    {
        List<Claim> claims = [new("sub", $"customer-{request.CustomerId}"), new("customer_id", request.CustomerId.ToString())];
        claims.AddRange((request.Roles ?? []).Select(role => new Claim("role", role)));

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = "bookstore-api",
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
        });
    }
}

// POST /e2e/token yalnız bu host'ta var; API'nin kendi pipeline'ının önüne eklenir
public sealed class E2ETokenEndpoint(E2ETokenIssuer issuer) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Map("/e2e/token", branch => branch.Run(async context =>
        {
            TokenRequest? request = await context.Request.ReadFromJsonAsync<TokenRequest>();
            await context.Response.WriteAsJsonAsync(new { accessToken = issuer.Issue(request!) });
        }));
        next(app);
    };
}
