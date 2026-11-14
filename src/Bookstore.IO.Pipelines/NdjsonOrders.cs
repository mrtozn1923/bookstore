using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;
using Bookstore.Ordering;

namespace Bookstore.IO.Pipelines;

public static class NdjsonOrders
{
    public const int MaxLineLength = 64 * 1024;

    // Classic approach: StreamReader turns every line into a string, then JSON parses the string.
    public static async Task<long> ReadWithStreamReaderAsync(Stream stream, Func<Order, ValueTask> onOrder,
        CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        long count = 0;
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (line.Length == 0) continue;
            await onOrder(JsonSerializer.Deserialize<Order>(line)!);
            count++;
        }
        return count;
    }

    // Pipelines approach: lines stay as UTF-8 bytes inside the pipe's pooled buffers.
    public static async Task<long> ReadWithPipeReaderAsync(PipeReader reader, Func<Order, ValueTask> onOrder,
        CancellationToken ct = default)
    {
        long count = 0;
        try
        {
            while (true)
            {
                ReadResult result = await reader.ReadAsync(ct);
                ReadOnlySequence<byte> buffer = result.Buffer;

                try
                {
                    while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
                    {
                        if (line.IsEmpty) continue;
                        await onOrder(ParseOrder(line));
                        count++;
                    }

                    if (buffer.Length > MaxLineLength)
                    {
                        throw new InvalidDataException($"Line longer than {MaxLineLength} bytes.");
                    }

                    if (result.IsCompleted)
                    {
                        if (!buffer.IsEmpty)
                        {
                            // The last line has no trailing '\n'.
                            await onOrder(ParseOrder(buffer));
                            count++;
                            buffer = buffer.Slice(buffer.End);
                        }
                        break;
                    }
                }
                finally
                {
                    // consumed: everything before the first unparsed byte; examined: everything we looked at.
                    reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
        }
        finally
        {
            await reader.CompleteAsync();
        }
        return count;
    }

    private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
        var sequenceReader = new SequenceReader<byte>(buffer);
        if (sequenceReader.TryReadTo(out line, (byte)'\n'))
        {
            buffer = buffer.Slice(sequenceReader.Position);
            return true;
        }
        line = default;
        return false;
    }

    private static Order ParseOrder(ReadOnlySequence<byte> line)
    {
        if (line.IsSingleSegment)
        {
            return JsonSerializer.Deserialize<Order>(line.FirstSpan)!;
        }
        // The line spans two buffer segments: Utf8JsonReader reads it without copying.
        var jsonReader = new Utf8JsonReader(line);
        return JsonSerializer.Deserialize<Order>(ref jsonReader)!;
    }
}
