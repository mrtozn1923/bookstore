namespace Bookstore.Api.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "";
    public string SigningKey { get; set; } = "";
}
