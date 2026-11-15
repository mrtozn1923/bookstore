using System.IO.Compression;
using System.Text;
using SkiaSharp;

namespace Bookstore.Api.Tests;

public static class TestFiles
{
    public static byte[] Jpeg(int width = 60, int height = 90)
    {
        using SKBitmap bitmap = new(width, height);
        bitmap.Erase(new SKColor(0x1E, 0x3A, 0x5F));
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    public static byte[] Pdf() => Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj << /Type /Catalog >> endobj\ntrailer << /Root 1 0 R >>\n%%EOF\n");

    public static byte[] Svg() => Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(document.domain)</script></svg>");

    // A real JPEG with an HTML payload appended after the image data
    public static byte[] Polyglot() => [.. Jpeg(), .. Encoding.ASCII.GetBytes("<html><script>alert(document.cookie)</script></html>")];

    // Built at run time so that this source file itself does not trigger the reader's antivirus
    public static byte[] Eicar() => Encoding.ASCII.GetBytes(@"X5O!P%@AP[4\PZX54(P^)7CC)7}$" + "EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*");

    public static byte[] Epub(params (string Name, byte[] Content)[] entries)
    {
        using MemoryStream output = new();
        using (ZipArchive zip = new(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            ZipArchiveEntry mimetype = zip.CreateEntry("mimetype", CompressionLevel.NoCompression);
            using (Stream s = mimetype.Open())
            {
                s.Write("application/epub+zip"u8);
            }

            foreach ((string name, byte[] content) in entries)
            {
                using Stream s = zip.CreateEntry(name, CompressionLevel.SmallestSize).Open();
                s.Write(content);
            }
        }

        return output.ToArray();
    }

    public static byte[] Chapter() => Encoding.UTF8.GetBytes("<html><body><h1>Dune</h1><p>Chapter 1</p></body></html>");
}
