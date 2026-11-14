using SkiaSharp;

namespace Bookstore.Functions;

public static class CoverThumbnail
{
    public const int Width = 320;

    // Keeps the aspect ratio; never enlarges a small cover
    public static (int Width, int Height) FitToWidth(int width, int height, int maxWidth)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Image size must be positive.");
        }
        if (width <= maxWidth)
        {
            return (width, height);
        }
        return (maxWidth, (int)Math.Round(height * (double)maxWidth / width));
    }

    public static byte[] CreateWebp(Stream source, int maxWidth = Width, int quality = 80)
    {
        using SKBitmap original = SKBitmap.Decode(source)
            ?? throw new InvalidDataException("The blob is not an image Skia can decode.");
        (int width, int height) = FitToWidth(original.Width, original.Height, maxWidth);
        using SKBitmap resized = original.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
        using SKImage image = SKImage.FromBitmap(resized);
        using SKData data = image.Encode(SKEncodedImageFormat.Webp, quality);
        return data.ToArray();
    }
}
