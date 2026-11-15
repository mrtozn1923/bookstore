using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // __Host- prefix: the browser accepts it only with Secure, Path=/ and no Domain
        options.Cookie.Name = "__Host-bookstore";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        // A fetch without a session gets 401 instead of a redirect to a login page
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

WebApplication app = builder.Build();

// The SPA's build output (npm run build → dist), served by the BFF from the same origin as /api and /bff
PhysicalFileProvider spaFiles = new(Path.GetFullPath(app.Configuration["Spa:Root"]!, app.Environment.ContentRootPath));
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = spaFiles,
    OnPrepareResponse = context => context.Context.Response.Headers.CacheControl =
        // Vite puts a content hash into every file name under /assets: such a file never changes
        context.Context.Request.Path.StartsWithSegments("/assets") ? "public, max-age=31536000, immutable" : "no-cache"
});

app.UseAuthentication();
app.UseAuthorization();

// Anti-CSRF: fetch calls to /api and /bff must send a custom header, which another site cannot send without a CORS preflight
app.Use(async (context, next) =>
{
    bool isCall = context.Request.Path.StartsWithSegments("/api")
        || (context.Request.Path.StartsWithSegments("/bff") && !context.Request.Path.StartsWithSegments("/bff/login"));
    if (isCall && context.Request.Headers["X-CSRF"] != "1")
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Missing X-CSRF header");
        return;
    }
    await next();
});

if (app.Environment.IsDevelopment())
{
    // Development only: stands in for the OpenID Connect redirect to an identity provider
    app.MapGet("/bff/login", async (string? returnUrl, HttpContext http) =>
    {
        ClaimsIdentity identity = new(
            [new Claim(ClaimTypes.Name, "Ada Lovelace"), new Claim(ClaimTypes.Email, "ada@example.com")],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(new ClaimsPrincipal(identity));
        // Only paths inside this site: no open redirect
        bool isLocal = returnUrl is not null && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\");
        return Results.Redirect(isLocal ? returnUrl! : "/");
    });
}

app.MapGet("/bff/user", (ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true
    ? Results.Ok(new { Name = user.Identity.Name, Email = user.FindFirstValue(ClaimTypes.Email) })
    : Results.Unauthorized());

app.MapPost("/bff/logout", async (HttpContext http) =>
{
    await http.SignOutAsync();
    return Results.NoContent();
});

// /api/... goes to Bookstore.Api; the routes and their authorization policies are in appsettings.json
app.MapReverseProxy();

// SPA fallback: /books/9780441172719 is not a file, so index.html answers and React Router takes over
app.MapFallbackToFile("index.html", new StaticFileOptions
{
    FileProvider = spaFiles,
    OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "no-cache"
});

app.Run();
