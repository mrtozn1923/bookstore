using System.Text;
using Bookstore.Notifications.Images;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Bookstore.Notifications.Tests;

public sealed class CoverImageTests
{
    private readonly CoverImageProcessor _processor = new(Options.Create(new CoverImageOptions { Widths = CoverImageOptions.DefaultWidths }));

    private static byte[] Asset(string name) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", name));

    [Fact]
    public void Sideways_phone_photo_becomes_an_upright_thumbnail_without_metadata()
    {
        byte[] original = Asset("cover-dune.jpg");
        Assert.Equal(SKEncodedOrigin.RightTop, _processor.Inspect(original).Origin);

        IReadOnlyList<CoverThumbnail> thumbnails = _processor.CreateThumbnails(original);

        Assert.Equal([160, 320, 640], thumbnails.Select(t => t.Width));
        CoverThumbnail medium = thumbnails[1];
        Assert.Equal((320, 480), (medium.Width, medium.Height));
        string[] chunks = [.. Chunks(medium.Content)];
        Assert.Contains(chunks, c => c.StartsWith("VP8", StringComparison.Ordinal));
        Assert.DoesNotContain("EXIF", chunks);
    }

    [Theory]
    [InlineData("bomb.png", "400,000,000 pixels")]
    [InlineData("not-an-image.jpg", "Not a JPEG, PNG or WebP file.")]
    public void Dangerous_uploads_are_rejected_before_decoding(string file, string reason)
    {
        InvalidImageException ex = Assert.Throws<InvalidImageException>(() => _processor.Inspect(Asset(file)));
        Assert.Contains(reason, ex.Message);
    }

    // RIFF chunk ids of a WebP file (VP8, VP8X, EXIF, XMP ...)
    private static IEnumerable<string> Chunks(byte[] webp)
    {
        for (int offset = 12; offset + 8 <= webp.Length;)
        {
            yield return Encoding.ASCII.GetString(webp, offset, 4);
            int size = BitConverter.ToInt32(webp, offset + 4);
            offset += 8 + size + (size & 1);
        }
    }
}
