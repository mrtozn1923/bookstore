using Bookstore.Admin.Client.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Admin.Identity;

public static class IdentitySetup
{
    public static WebApplicationBuilder AddPanelIdentity(this WebApplicationBuilder builder, string connectionString)
    {
        builder.Services.AddDbContext<AdminIdentityContext>(options => options.UseSqlServer(
            connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));

        builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/account/login";
            options.AccessDeniedPath = "/account/access-denied";
        });
        builder.Services.AddIdentityCore<IdentityUser>(options => options.User.RequireUniqueEmail = true)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AdminIdentityContext>()
            .AddSignInManager();

        // Every endpoint needs a staff member unless it says [AllowAnonymous].
        builder.Services.AddAuthorization(options =>
        {
            options.AddPanelPolicies();
            options.FallbackPolicy = options.GetPolicy(PanelPolicies.Staff);
        });
        builder.Services.AddCascadingAuthenticationState();

        builder.Services.AddOptions<PanelTokenOptions>()
            .BindConfiguration("PanelTokens")
            .Validate(o => o.SigningKey.Length >= 32, "PanelTokens:SigningKey must be at least 32 characters (same value as Bookstore.Api).")
            .ValidateOnStart();
        builder.Services.AddSingleton<PanelTokenIssuer>();
        return builder;
    }

    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        // A form post with an antiforgery token ([FromForm] turns the check on), never a GET link.
        app.MapPost("/account/logout", async (SignInManager<IdentityUser> signIn, [FromForm] string returnUrl) =>
        {
            await signIn.SignOutAsync();
            return TypedResults.LocalRedirect($"~/account/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        });
        return app;
    }

    // Development only: the identity schema and two staff members with the password from configuration.
    public static async Task SeedDevelopmentStaffAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AdminIdentityContext>().Database.MigrateAsync();

        RoleManager<IdentityRole> roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (string role in new[] { PanelPolicies.StoreStaff, PanelPolicies.WarehouseStaff, PanelPolicies.Admin })
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole(role));
            }
        }

        string password = app.Configuration["Identity:SeedPassword"]
            ?? throw new InvalidOperationException("Identity:SeedPassword is missing (appsettings.Development.json).");
        UserManager<IdentityUser> users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        (string Email, string Role)[] staff =
            [("grace@example.com", PanelPolicies.StoreStaff), ("linus@example.com", PanelPolicies.WarehouseStaff)];
        foreach ((string email, string role) in staff)
        {
            if (await users.FindByEmailAsync(email) is null)
            {
                IdentityUser user = new() { UserName = email, Email = email, EmailConfirmed = true };
                IdentityResult created = await users.CreateAsync(user, password);
                if (!created.Succeeded)
                {
                    throw new InvalidOperationException(string.Join(" ", created.Errors.Select(e => e.Description)));
                }
                await users.AddToRoleAsync(user, role);
            }
        }
    }
}
