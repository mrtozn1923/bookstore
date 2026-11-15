using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Bookstore.Auth;

// Roles and policies of the back-office endpoints. The API owns these rules; the panel only mirrors them in its UI.
public static class StaffRoles
{
    public const string StoreStaff = "StoreStaff";
    public const string WarehouseStaff = "WarehouseStaff";
    public const string Admin = "Admin";
}

public static class ApiPolicies
{
    public const string Staff = "Staff";
    public const string ManageCatalog = "ManageCatalog";
}

public static class PanelAuthentication
{
    // Bookstore.Admin signs a short-lived token for the signed-in staff member; the API validates it.
    public static WebApplicationBuilder AddPanelAuthentication(this WebApplicationBuilder builder)
    {
        IConfigurationSection tokens = builder.Configuration.GetSection("PanelTokens");
        string signingKey = tokens["SigningKey"] is { Length: >= 32 } key
            ? key
            : throw new InvalidOperationException(
                "PanelTokens:SigningKey is missing or shorter than 32 characters. It must match Bookstore.Admin's value.");

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = tokens["Issuer"],
                    ValidAudience = tokens["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(ApiPolicies.Staff, policy => policy
                .RequireRole(StaffRoles.StoreStaff, StaffRoles.WarehouseStaff, StaffRoles.Admin))
            .AddPolicy(ApiPolicies.ManageCatalog, policy => policy
                .RequireRole(StaffRoles.StoreStaff, StaffRoles.Admin));
        return builder;
    }
}
