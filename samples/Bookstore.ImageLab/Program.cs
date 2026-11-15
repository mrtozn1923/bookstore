using System.Diagnostics;
using System.Text;
using Bookstore.Notifications.Images;
using Microsoft.Extensions.Options;
using SkiaSharp;

string command = args.FirstOrDefault() ?? "thumbs";
string file = args.Length > 1 ? args[1] : "../../tests/Bookstore.Notifications.Tests/Assets/cover-dune.jpg";
Directory.CreateDirectory("out");
CoverImageProcessor processor = new(Options.Create(new CoverImageOptions { Widths = CoverImageOptions.DefaultWidths }));
byte[] bytes = await File.ReadAllBytesAsync(file);

switch (command)
{
    case "inspect":
    {
        Stopwatch watch = Stopwatch.StartNew();
        try
        {
            ImageCheck check = processor.Inspect(bytes);
            Console.WriteLine($"{Path.GetFileName(file)}: {bytes.Length:N0} bytes, {check.Format} {check.Width}x{check.Height}, origin={check.Origin} ({watch.Elapsed.TotalMilliseconds:F1} ms)");
        }
        catch (InvalidImageException ex)
        {
            Console.WriteLine($"{Path.GetFileName(file)}: rejected in {watch.Elapsed.TotalMilliseconds:F1} ms: {ex.Message}");
        }

        break;
    }
    case "thumbs":
    {
        Stopwatch watch = Stopwatch.StartNew();
        IReadOnlyList<CoverThumbnail> thumbnails = processor.CreateThumbnails(bytes);
        Console.WriteLine($"original: {bytes.Length:N0} bytes, EXIF={Contains(bytes, "Exif")}");
        foreach (CoverThumbnail t in thumbnails)
        {
            await File.WriteAllBytesAsync($"out/dune-{t.Width}{t.Extension}", t.Content);
            Console.WriteLine($"{t.Width}x{t.Height} {t.ContentType} {t.Content.Length:N0} bytes, EXIF={Contains(t.Content, "EXIF") || Contains(t.Content, "Exif")}");
        }

        Console.WriteLine($"3 thumbnails in {watch.ElapsedMilliseconds} ms");
        break;
    }
    case "formats":
    {
        // Which encoders this SkiaSharp build ships with
        using SKBitmap bitmap = SKBitmap.Decode(bytes);
        using SKImage image = SKImage.FromBitmap(bitmap.Resize(new SKImageInfo(320, 480), new SKSamplingOptions(SKCubicResampler.Mitchell)));
        foreach (SKEncodedImageFormat format in new[] { SKEncodedImageFormat.Jpeg, SKEncodedImageFormat.Png, SKEncodedImageFormat.Webp, SKEncodedImageFormat.Avif })
        {
            using SKData? encoded = image.Encode(format, 80);
            Console.WriteLine($"{format}: {(encoded is null ? "no encoder (null)" : $"{encoded.Size:N0} bytes")}");
        }

        break;
    }
}

static bool Contains(byte[] data, string marker) => data.AsSpan().IndexOf(Encoding.ASCII.GetBytes(marker)) >= 0;

