using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Bookstore.Admin.Identity;

public sealed class PanelTokenOptions
{
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public string SigningKey { get; set; } = "";
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(5);
}

// Signs a short-lived token for the signed-in staff member; Bookstore.Api validates it with the same key.
public sealed class PanelTokenIssuer(IOptions<PanelTokenOptions> options, TimeProvider time)
{
    private readonly JsonWebTokenHandler handler = new();

    public string CreateToken(ClaimsPrincipal user)
    {
        PanelTokenOptions o = options.Value;
        List<Claim> claims =
        [
            new("sub", user.FindFirstValue(ClaimTypes.NameIdentifier) ?? ""),
            new("name", user.Identity?.Name ?? ""),
            .. user.FindAll(ClaimTypes.Role).Select(r => new Claim("role", r.Value))
        ];
        DateTime now = time.GetUtcNow().UtcDateTime;
        return handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = o.Issuer,
            Audience = o.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.Add(o.Lifetime),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.SigningKey)), SecurityAlgorithms.HmacSha256)
        });
    }
}
