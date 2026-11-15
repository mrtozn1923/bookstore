using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Bookstore.Admin.Tests;

public sealed class PanelTokenIssuerTests
{
    [Fact]
    public async Task The_token_carries_the_user_name_and_roles_the_api_checks()
    {
        string token = TestStaff.Tokens().CreateToken(TestStaff.User("grace@example.com", "StoreStaff", "Admin"));

        TokenValidationResult result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = "bookstore-admin",
            ValidAudience = "bookstore-api",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestStaff.SigningKey)),
            NameClaimType = "name",
            RoleClaimType = "role"
        });

        Assert.True(result.IsValid);
        Assert.Equal("grace@example.com", result.ClaimsIdentity.Name);
        Assert.Equal(["StoreStaff", "Admin"], result.ClaimsIdentity.FindAll("role").Select(c => c.Value));
        JsonWebToken jwt = (JsonWebToken)result.SecurityToken;
        Assert.Equal(TimeSpan.FromMinutes(5), jwt.ValidTo - jwt.ValidFrom);
    }
}
