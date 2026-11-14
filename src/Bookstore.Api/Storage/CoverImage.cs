namespace Bookstore.Storage;

public sealed record CoverFormat(string ContentType, string Extension);

public static class CoverImage
{
    public const long MaxBytes = 2 * 1024 * 1024;

    private static readonly CoverFormat Jpeg = new("image/jpeg", ".jpg");
    private static readonly CoverFormat Png = new("image/png", ".png");
    private static readonly CoverFormat WebP = new("image/webp", ".webp");

    // The file name and the Content-Type header come from the client; only the first bytes are trusted
    public static CoverFormat? Detect(ReadOnlySpan<byte> header) => header switch
    {
        [0xFF, 0xD8, 0xFF, ..] => Jpeg,
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, ..] => Png,
        [0x52, 0x49, 0x46, 0x46, _, _, _, _, 0x57, 0x45, 0x42, 0x50, ..] => WebP,
        _ => null,
    };
}
