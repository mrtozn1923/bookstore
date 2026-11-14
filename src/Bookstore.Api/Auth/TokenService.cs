using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Bookstore.Api.Auth;

public record TokenResponse(string AccessToken, DateTime ExpiresAt);

public interface ITokenService
{
    TokenResponse CreateAccessToken(AppUser user);
}

public class TokenService : ITokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public TokenResponse CreateAccessToken(AppUser user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("name", user.DisplayName)
        };
        // every role becomes a separate "role" claim
        claims.AddRange(user.Roles.Select(role => new Claim("role", role)));
        if (user.StaffSince is not null)
        {
            claims.Add(new Claim("staff_since", user.StaffSince.Value.ToString("yyyy-MM-dd")));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(claims)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new TokenResponse(token, expiresAt);
    }
}
