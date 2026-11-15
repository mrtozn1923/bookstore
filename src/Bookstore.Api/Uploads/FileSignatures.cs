namespace Bookstore.Api.Uploads;

public static class FileSignatures
{
    // Decides by the first bytes, not by the file name or the Content-Type the client sent
    public static FileFormat? Detect(ReadOnlySpan<byte> head) => head switch
    {
        [0xFF, 0xD8, 0xFF, ..] => FileFormat.Jpeg,
        [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A, ..] => FileFormat.Png,
        [(byte)'R', (byte)'I', (byte)'F', (byte)'F', _, _, _, _, (byte)'W', (byte)'E', (byte)'B', (byte)'P', ..] => FileFormat.WebP,
        [(byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-', ..] => FileFormat.Pdf,
        [(byte)'P', (byte)'K', 0x03, 0x04, ..] when IsEpub(head) => FileFormat.Epub,
        _ => null,
    };

    // EPUB (OCF): the first zip entry is an uncompressed "mimetype" file with no extra field
    private static bool IsEpub(ReadOnlySpan<byte> head) =>
        head.Length >= 58
        && head[8] == 0 && head[9] == 0
        && head[28] == 0 && head[29] == 0
        && head[30..38].SequenceEqual("mimetype"u8)
        && head[38..58].SequenceEqual("application/epub+zip"u8);

    public static bool IsAllowed(UploadPurpose purpose, FileFormat format) => (purpose, format) switch
    {
        (UploadPurpose.Cover, FileFormat.Jpeg or FileFormat.Png or FileFormat.WebP) => true,
        (UploadPurpose.SampleChapter, FileFormat.Pdf or FileFormat.Epub) => true,
        _ => false,
    };
}

public static class UploadFileName
{
    // Only for display and logs: no path, no control characters, bounded length
    public static string ForDisplay(string? name)
    {
        string clean = Printable(Path.GetFileName((name ?? "").Replace('\\', '/')));
        return clean.Length == 0 ? "unnamed" : clean;
    }

    public static string Printable(string text) => new([.. text.Where(c => !char.IsControl(c)).Take(100)]);
}
