using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Bookstore.Api.Uploads;

public sealed class CoverReencoder(IOptions<UploadOptions> options)
{
    public byte[] Reencode(byte[] original)
    {
        using SKData data = SKData.CreateCopy(original);
        using SKCodec codec = SKCodec.Create(data) ?? throw new InvalidUploadException("The image could not be read.");

        // Header only: the pixel limit is checked before a single pixel is decoded
        long pixels = (long)codec.Info.Width * codec.Info.Height;
        if (pixels > options.Value.MaxCoverPixels)
        {
            throw new InvalidUploadException($"{codec.Info.Width}x{codec.Info.Height} is {pixels:N0} pixels, the limit is {options.Value.MaxCoverPixels:N0}.");
        }

        // Decode to raw pixels and write a brand-new file: nothing else from the original survives
        using SKBitmap bitmap = SKBitmap.Decode(codec) ?? throw new InvalidUploadException("The image could not be decoded.");
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData webp = image.Encode(SKEncodedImageFormat.Webp, 85);
        return webp.ToArray();
    }
}
