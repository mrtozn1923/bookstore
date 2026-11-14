using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Bookstore.Ordering;

namespace Bookstore.IO.Streams;

public static class OrderExport
{
    // FileStream <- GZipStream <- StreamWriter: each order is one JSON line.
    public static async Task WriteAsync(string path, IEnumerable<Order> orders, CancellationToken ct = default)
    {
        await using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 4096, useAsync: true);
        await using var gzip = new GZipStream(file, CompressionLevel.Optimal);
        await using var writer = new StreamWriter(gzip, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        foreach (var order in orders)
        {
            await writer.WriteLineAsync(JsonSerializer.Serialize(order).AsMemory(), ct);
        }
    }

    // The same chain in reverse: FileStream -> GZipStream -> StreamReader.
    public static async IAsyncEnumerable<Order> ReadAsync(string path,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);
        await using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            yield return JsonSerializer.Deserialize<Order>(line)!;
        }
    }
}
