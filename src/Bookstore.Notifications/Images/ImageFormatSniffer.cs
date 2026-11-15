namespace Bookstore.Notifications.Images;

public enum UploadedImageFormat { Jpeg, Png, WebP }

// Decides by the first bytes of the file, never by its name or the Content-Type the client sent
public static class ImageFormatSniffer
{
    public const int HeaderLength = 12;

    public static UploadedImageFormat? Detect(ReadOnlySpan<byte> header) => header switch
    {
        [0xFF, 0xD8, 0xFF, ..] => UploadedImageFormat.Jpeg,
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, ..] => UploadedImageFormat.Png,
        [(byte)'R', (byte)'I', (byte)'F', (byte)'F', _, _, _, _, (byte)'W', (byte)'E', (byte)'B', (byte)'P', ..] => UploadedImageFormat.WebP,
        _ => null,
    };
}
