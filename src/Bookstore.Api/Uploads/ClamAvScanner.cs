using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Uploads;

public sealed record ScanResult(bool IsClean, string? Signature);

public sealed class ClamAvScanner(IOptions<ClamAvOptions> options)
{
    public async Task<ScanResult> ScanAsync(Stream content, CancellationToken ct)
    {
        ClamAvOptions o = options.Value;
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(o.Timeout);

        using TcpClient client = new();
        await client.ConnectAsync(o.Host, o.Port, timeout.Token);
        await using NetworkStream stream = client.GetStream();

        // INSTREAM: 4-byte big-endian length + data per chunk, a zero length ends the stream
        await stream.WriteAsync("zINSTREAM\0"u8.ToArray(), timeout.Token);
        byte[] buffer = new byte[64 * 1024];
        byte[] length = new byte[4];
        int read;
        while ((read = await content.ReadAsync(buffer, timeout.Token)) > 0)
        {
            BinaryPrimitives.WriteUInt32BigEndian(length, (uint)read);
            await stream.WriteAsync(length, timeout.Token);
            await stream.WriteAsync(buffer.AsMemory(0, read), timeout.Token);
        }

        BinaryPrimitives.WriteUInt32BigEndian(length, 0);
        await stream.WriteAsync(length, timeout.Token);

        using StreamReader reader = new(stream, Encoding.ASCII);
        string reply = (await reader.ReadToEndAsync(timeout.Token)).TrimEnd('\0');
        return reply switch
        {
            "stream: OK" => new ScanResult(true, null),
            _ when reply.StartsWith("stream: ") && reply.EndsWith(" FOUND") => new ScanResult(false, reply["stream: ".Length..^" FOUND".Length]),
            _ => throw new ScannerUnavailableException($"Unexpected clamd reply: {reply}"),
        };
    }
}
