using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Bookstore.Identity;

public sealed record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);

public sealed record CurrentUser(string Email, string Name, string Role);

public sealed record JwtSigningKey(SymmetricSecurityKey Key);

public static class AuthModule
{
    public static IServiceCollection AddAuthModule(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        IConfigurationSection section = configuration.GetSection(AuthOptions.SectionName);
        services.Configure<AuthOptions>(section);
        AuthOptions options = section.Get<AuthOptions>() ?? new AuthOptions();

        JwtSigningKey signingKey = new(new SymmetricSecurityKey(ResolveKey(options, environment)));
        services.AddSingleton(signingKey);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = options.Issuer,
                    ValidAudience = options.Audience,
                    IssuerSigningKey = signingKey.Key,
                    NameClaimType = "name",
                    RoleClaimType = "role",
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });
        services.AddAuthorization();
        return services;
    }

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder auth = app.MapGroup("/api/auth").WithTags("Identity");
        auth.MapPost("/login", Login);
        auth.MapGet("/me", Me).RequireAuthorization();
        return app;
    }

    // POST /api/auth/login  { "email": "...", "password": "..." }  → bearer token
    private static Results<Ok<TokenResponse>, ProblemHttpResult> Login(
        LoginRequest request, IOptions<AuthOptions> options, JwtSigningKey signingKey, TimeProvider clock)
    {
        AuthOptions auth = options.Value;
        DemoUser? user = auth.DemoUsers.FirstOrDefault(u =>
            string.Equals(u.Email, request.Email, StringComparison.OrdinalIgnoreCase));

        if (user is null || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(user.Password), Encoding.UTF8.GetBytes(request.Password)))
        {
            return TypedResults.Problem("Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;
        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = auth.Issuer,
            Audience = auth.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.Add(auth.TokenLifetime),
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", user.Email),
                new Claim("email", user.Email),
                new Claim("name", user.Name),
                new Claim("role", user.Role)
            ]),
            SigningCredentials = new SigningCredentials(signingKey.Key, SecurityAlgorithms.HmacSha256)
        };

        string token = new JsonWebTokenHandler().CreateToken(descriptor);
        return TypedResults.Ok(new TokenResponse(token, "Bearer", (int)auth.TokenLifetime.TotalSeconds));
    }

    private static Ok<CurrentUser> Me(ClaimsPrincipal user) =>
        TypedResults.Ok(new CurrentUser(
            user.FindFirstValue("email") ?? "",
            user.Identity?.Name ?? "",
            user.FindFirstValue("role") ?? ""));

    private static byte[] ResolveKey(AuthOptions options, IHostEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(options.SigningKey))
        {
            byte[] key = Convert.FromBase64String(options.SigningKey);
            return key.Length >= 32
                ? key
                : throw new InvalidOperationException("Auth:SigningKey must be at least 32 bytes (Base64).");
        }

        // Development: a new key on every start, so tokens from the previous run stop working.
        return environment.IsDevelopment()
            ? RandomNumberGenerator.GetBytes(32)
            : throw new InvalidOperationException("Auth:SigningKey is missing.");
    }
}
