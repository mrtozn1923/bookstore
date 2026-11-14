using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

// Development only: issues a token signed with the same fake key as appsettings.Development.json.
// In production tokens come from your identity provider, never from a tool like this.
// usage: dotnet run --project tools/Bookstore.DevToken -- <userId> [role=Customer] [minutes=30]
const string Issuer = "https://identity.bookstore.test";
const string Audience = "bookstore-api";
const string DevSigningKey = "dev-only-bookstore-signing-key-not-a-secret-0123456789";

string userId = args.Length > 0 ? args[0] : "ada";
string role = args.Length > 1 ? args[1] : "Customer";
int minutes = args.Length > 2 ? int.Parse(args[2]) : 30;

SecurityTokenDescriptor descriptor = new()
{
    Issuer = Issuer,
    Audience = Audience,
    Expires = DateTime.UtcNow.AddMinutes(minutes),
    Subject = new ClaimsIdentity(
    [
        new Claim("sub", userId),
        new Claim("name", userId),
        new Claim("role", role),
    ]),
    SigningCredentials = new SigningCredentials(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DevSigningKey)), SecurityAlgorithms.HmacSha256),
};

Console.WriteLine(new JsonWebTokenHandler().CreateToken(descriptor));
