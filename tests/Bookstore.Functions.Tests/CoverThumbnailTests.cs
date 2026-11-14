using SkiaSharp;

namespace Bookstore.Functions.Tests;

public class CoverThumbnailTests
{
    [Theory]
    [InlineData(1200, 1800, 320, 480)]   // typical portrait cover
    [InlineData(1600, 900, 320, 180)]    // landscape box set image
    [InlineData(200, 300, 200, 300)]     // smaller than the target: unchanged
    public void FitToWidth_keeps_the_aspect_ratio(int width, int height, int expectedWidth, int expectedHeight)
    {
        Assert.Equal((expectedWidth, expectedHeight), CoverThumbnail.FitToWidth(width, height, 320));
    }

    [Fact]
    public void CreateWebp_resizes_a_jpeg_cover()
    {
        using SKBitmap bitmap = new(1200, 1800);
        using (SKCanvas canvas = new(bitmap))
        {
            canvas.Clear(SKColors.SteelBlue);
        }
        using SKData jpeg = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Jpeg, 90);
        using MemoryStream source = new(jpeg.ToArray());

        byte[] webp = CoverThumbnail.CreateWebp(source);

        using SKBitmap thumbnail = SKBitmap.Decode(webp);
        Assert.Equal((320, 480), (thumbnail.Width, thumbnail.Height));
    }

    [Fact]
    public void CreateWebp_rejects_a_file_that_is_not_an_image()
    {
        using MemoryStream source = new("not an image"u8.ToArray());

        Assert.Throws<InvalidDataException>(() => CoverThumbnail.CreateWebp(source));
    }
}
