using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Bookstore.Notifications.Images;

public sealed class CoverImageOptions
{
    [Range(1, 50 * 1024 * 1024)]
    public int MaxBytes { get; set; } = 10 * 1024 * 1024;

    // Width x height of the original; a 40 MP image is already 160 MB once decoded to RGBA
    [Range(1, 100_000_000)]
    public long MaxPixels { get; set; } = 40_000_000;

    public int[] Widths { get; set; } = [];

    public static readonly int[] DefaultWidths = [160, 320, 640];

    [Range(1, 100)]
    public int WebPQuality { get; set; } = 80;
}

public sealed record CoverThumbnail(int Width, int Height, string ContentType, string Extension, byte[] Content);

public sealed class InvalidImageException(string message) : Exception(message);

// SkiaSharp (MIT): validate, apply EXIF orientation, resize, re-encode without metadata
public sealed class CoverImageProcessor(IOptions<CoverImageOptions> options)
{
    public ImageCheck Inspect(ReadOnlySpan<byte> file)
    {
        CoverImageOptions o = options.Value;
        if (file.Length > o.MaxBytes)
        {
            throw new InvalidImageException($"File is {file.Length:N0} bytes, the limit is {o.MaxBytes:N0}.");
        }

        if (ImageFormatSniffer.Detect(file) is not { } format)
        {
            throw new InvalidImageException("Not a JPEG, PNG or WebP file.");
        }

        // Reads only the header: width and height are known before a single pixel is decoded
        using SKData data = SKData.CreateCopy(file);
        using SKCodec? codec = SKCodec.Create(data);
        if (codec is null)
        {
            throw new InvalidImageException($"The {format} file could not be read.");
        }

        long pixels = (long)codec.Info.Width * codec.Info.Height;
        if (pixels > o.MaxPixels)
        {
            throw new InvalidImageException($"{codec.Info.Width}x{codec.Info.Height} is {pixels:N0} pixels, the limit is {o.MaxPixels:N0}.");
        }

        return new ImageCheck(format, codec.Info.Width, codec.Info.Height, codec.EncodedOrigin);
    }

    public IReadOnlyList<CoverThumbnail> CreateThumbnails(ReadOnlySpan<byte> file)
    {
        ImageCheck check = Inspect(file);
        using SKData data = SKData.CreateCopy(file);
        using SKCodec codec = SKCodec.Create(data)!;
        using SKBitmap decoded = SKBitmap.Decode(codec);
        using SKBitmap upright = ApplyOrigin(decoded, check.Origin);

        List<CoverThumbnail> thumbnails = [];
        foreach (int width in options.Value.Widths.Where(w => w < upright.Width))
        {
            int height = (int)Math.Round(upright.Height * (double)width / upright.Width);
            using SKBitmap resized = upright.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
            using SKImage image = SKImage.FromBitmap(resized);
            using SKData webp = image.Encode(SKEncodedImageFormat.Webp, options.Value.WebPQuality);
            thumbnails.Add(new CoverThumbnail(width, height, "image/webp", ".webp", webp.ToArray()));
        }

        return thumbnails;
    }

    // Phone photos are often stored sideways; the EXIF Orientation tag says how to turn them
    private static SKBitmap ApplyOrigin(SKBitmap source, SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft)
        {
            return source.Copy();
        }

        bool swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        SKBitmap target = new(swap ? source.Height : source.Width, swap ? source.Width : source.Height);
        using SKCanvas canvas = new(target);
        canvas.SetMatrix(OriginMatrix(origin, source.Width, source.Height));
        using SKImage image = SKImage.FromBitmap(source);
        canvas.DrawImage(image, 0, 0, SKSamplingOptions.Default);
        return target;
    }

    private static SKMatrix OriginMatrix(SKEncodedOrigin origin, int w, int h) => origin switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),
        _ => SKMatrix.Identity,
    };
}

public sealed record ImageCheck(UploadedImageFormat Format, int Width, int Height, SKEncodedOrigin Origin);
