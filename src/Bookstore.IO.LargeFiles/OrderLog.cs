using System.Buffers.Text;

namespace Bookstore.IO.LargeFiles;

// One line per order event: 2026-03-14T08:15:42Z,BS-012345678,Shipped,1116.40
public static class OrderLog
{
    public static readonly DateOnly FirstDay = new(2026, 1, 1);
    private static readonly string[] Statuses = ["Pending", "Cancelled", "Paid", "Shipped", "Delivered"];

    public static long Generate(string path, long targetBytes)
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 1 << 20);
        Span<byte> line = stackalloc byte[64];
        long lines = 0;
        while (file.Position < targetBytes)
        {
            lines++;
            int n = 0;
            DateTime time = start.AddTicks(lines * TimeSpan.TicksPerSecond / 4);
            Utf8Formatter.TryFormat(time, line, out int w, new System.Buffers.StandardFormat('O'));
            // "O" yedi haneli kesir yazar; saniyeye kadar tutup Z ekliyoruz.
            n = 19;
            line[n++] = (byte)'Z';
            line[n++] = (byte)',';
            "BS-"u8.CopyTo(line[n..]); n += 3;
            Utf8Formatter.TryFormat(lines, line[n..], out w, new System.Buffers.StandardFormat('D', 9)); n += w;
            line[n++] = (byte)',';
            ulong h = (ulong)lines * 2654435761UL;
            string status = Statuses[(int)(h % 10) switch { 0 => 0, 1 => 1, var x => 2 + x % 3 }];
            n += System.Text.Encoding.ASCII.GetBytes(status, line[n..]);
            line[n++] = (byte)',';
            long cents = 5_000 + (long)(h >> 7) % 200_000;
            Utf8Formatter.TryFormat(cents / 100, line[n..], out w); n += w;
            line[n++] = (byte)'.';
            Utf8Formatter.TryFormat(cents % 100, line[n..], out w, new System.Buffers.StandardFormat('D', 2)); n += w;
            line[n++] = (byte)'\n';
            file.Write(line[..n]);
        }
        return lines;
    }
}
