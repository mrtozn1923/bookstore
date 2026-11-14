using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Bookstore.Bff.Security;

public static class BffSecurity
{
    // The browser gets only an HttpOnly cookie. Tokens stay in the BFF's session and never reach JavaScript.
    public static IServiceCollection AddBffAuthentication(this IServiceCollection services, IConfiguration configuration, bool isProduction)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                // An API call without a session gets 401; only /bff/login sends the browser to Keycloak.
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = "bookstore.session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = isProduction ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
            })
            .AddKeycloakOpenIdConnect("keycloak", realm: "bookstore", OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = "bookstore-bff";
                options.ClientSecret = configuration["Keycloak:ClientSecret"];
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "roles";
                options.RequireHttpsMetadata = isProduction;
            });
        services.AddAuthorization();
        return services;
    }

    public static IEndpointRouteBuilder MapBffEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder bff = app.MapGroup("/bff");

        bff.MapGet("/login", (string? returnUrl) => Results.Challenge(
            new AuthenticationProperties { RedirectUri = returnUrl is { Length: > 0 } && returnUrl.StartsWith('/') ? returnUrl : "/" },
            [OpenIdConnectDefaults.AuthenticationScheme]));

        bff.MapPost("/logout", () => Results.SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
            .RequireAuthorization();

        // What Angular may know about the user: claims, never tokens.
        bff.MapGet("/user", (ClaimsPrincipal user) => Results.Ok(new
        {
            Name = user.FindFirstValue("name"),
            Email = user.FindFirstValue("email"),
            Roles = user.FindAll("roles").Select(r => r.Value)
        })).RequireAuthorization();

        return app;
    }

    // Cookies are sent with every request the browser makes, also from other sites. A state-changing API call
    // must carry a custom header, which a cross-site form or image cannot add.
    public static IApplicationBuilder UseAntiforgeryHeader(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api") &&
                !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) &&
                context.Request.Headers["X-CSRF"] != "1")
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { title = "Missing X-CSRF header" });
                return;
            }
            await next(context);
        });
}
