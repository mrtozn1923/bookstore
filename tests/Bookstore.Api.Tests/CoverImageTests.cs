using Bookstore.Storage;

namespace Bookstore.Api.Tests;

public class CoverImageTests
{
    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10 }, "image/jpeg")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D }, "image/png")]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 0x24, 0, 0, 0, 0x57, 0x45, 0x42, 0x50 }, "image/webp")]
    public void Detects_image_formats_from_their_first_bytes(byte[] header, string contentType) =>
        Assert.Equal(contentType, CoverImage.Detect(header)?.ContentType);

    [Fact]
    public void Rejects_a_text_file_even_if_it_is_named_jpg() =>
        Assert.Null(CoverImage.Detect("<html>hello</html>"u8));

    [Fact]
    public void Rejects_a_truncated_header() =>
        Assert.Null(CoverImage.Detect([0x89, 0x50]));
}
