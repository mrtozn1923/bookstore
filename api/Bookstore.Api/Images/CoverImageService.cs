using ImageMagick;
using Microsoft.Extensions.Caching.Memory;

namespace Bookstore.Api.Images;

public sealed record EncodedImage(byte[] Bytes, string ContentType);

// Resizes the original cover (Covers/*.jpg) and encodes it as AVIF, WebP or JPEG.
// Results are kept in a size-limited memory cache; in production a CDN or a disk cache sits in front.
public sealed class CoverImageService(IWebHostEnvironment env, IMemoryCache cache)
{
    // Only these widths are produced: an arbitrary ?w= would let anyone fill the cache and burn CPU
    public static readonly int[] Widths = [160, 320, 480, 640, 960, 1200];

    public async Task<EncodedImage?> GetAsync(string name, int width, string format, CancellationToken ct)
    {
        string path = Path.Combine(env.ContentRootPath, "Covers", name + ".jpg");
        if (!File.Exists(path))
        {
            return null;
        }
        string key = $"cover:{name}:{width}:{format}";
        return await cache.GetOrCreateAsync(key, async entry =>
        {
            EncodedImage image = await EncodeAsync(path, width, format, ct);
            entry.Size = image.Bytes.Length;
            return image;
        });
    }

    private static async Task<EncodedImage> EncodeAsync(string path, int width, string format, CancellationToken ct)
    {
        using MagickImage image = new(await File.ReadAllBytesAsync(path, ct));
        image.Resize((uint)width, 0);
        image.Strip();
        (MagickFormat magickFormat, uint quality, string contentType) = format switch
        {
            "avif" => (MagickFormat.Avif, 50u, "image/avif"),
            "webp" => (MagickFormat.WebP, 75u, "image/webp"),
            _ => (MagickFormat.Jpeg, 75u, "image/jpeg"),
        };
        image.Quality = quality;
        return new EncodedImage(image.ToByteArray(magickFormat), contentType);
    }
}
