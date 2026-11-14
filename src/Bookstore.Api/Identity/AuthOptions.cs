namespace Bookstore.Identity;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = "bookstore-api";
    public string Audience { get; set; } = "bookstore-clients";

    // Base64 encoded, at least 32 bytes. Leave empty in Development: a random key is generated at startup.
    public string? SigningKey { get; set; }

    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    // Development only: demo users for API tools. Real users come from the Identity module.
    public List<DemoUser> DemoUsers { get; set; } = [];
}

public sealed class DemoUser
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Name { get; set; }
    public required string Role { get; set; }
}
