using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required, Url]
    public string Issuer { get; set; } = "";

    // HMAC-SHA256 için en az 256 bit: base64'te 44 karakter
    [Required, MinLength(44)]
    public string SigningKey { get; set; } = "";
}
