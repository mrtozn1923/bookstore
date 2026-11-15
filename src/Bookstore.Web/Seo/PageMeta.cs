namespace Bookstore.Web.Seo;

public sealed record PageMeta(
    string Lang,
    string Title,
    string Description,
    string Canonical,
    string AlternateTr,
    string AlternateEn,
    string? OgImage = null,
    string? JsonLd = null,
    string Robots = "max-image-preview:large");
