using System.Buffers;
using System.Text.Json;
using Bookstore.Ordering;

namespace Bookstore.IO.Json.Streaming;

public static class OrderExportJson
{
    // Writes a JSON array without ever holding the whole list in memory.
    public static async Task WriteAsync(Stream destination, IAsyncEnumerable<Order> orders, CancellationToken ct = default) =>
        await JsonSerializer.SerializeAsync(destination, orders, BookstoreJsonContext.Default.IAsyncEnumerableOrder, ct);

    // Reads the array element by element; memory stays flat regardless of file size.
    public static async Task<ExportSummary> SummarizeAsync(Stream source, CancellationToken ct = default)
    {
        long count = 0;
        decimal revenue = 0;
        var customers = new HashSet<string>();
        await foreach (Order? order in JsonSerializer.DeserializeAsyncEnumerable(source, BookstoreJsonContext.Default.Order, ct))
        {
            count++;
            revenue += order!.Total;
            customers.Add(order.CustomerName);
        }
        return new ExportSummary(count, revenue, customers.Count);
    }

    // Lowest level: Utf8JsonReader over a refilled buffer, no Order objects at all.
    public static decimal SumLineTotals(Stream source)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        try
        {
            int filled = source.Read(buffer);
            var state = new JsonReaderState();
            decimal sum = 0;
            bool expectLineTotal = false;

            while (true)
            {
                bool isFinal = filled < buffer.Length && source.Position == source.Length;
                var reader = new Utf8JsonReader(buffer.AsSpan(0, filled), isFinal, state);

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        expectLineTotal = reader.ValueTextEquals("lineTotal"u8);
                    }
                    else if (expectLineTotal && reader.TokenType == JsonTokenType.Number)
                    {
                        sum += reader.GetDecimal();
                        expectLineTotal = false;
                    }
                }

                if (isFinal) return sum;

                // Keep the unread tail, read more after it.
                state = reader.CurrentState;
                int consumed = (int)reader.BytesConsumed;
                int leftover = filled - consumed;
                buffer.AsSpan(consumed, leftover).CopyTo(buffer);
                if (leftover == buffer.Length)
                {
                    throw new InvalidDataException("A single JSON token is larger than the buffer.");
                }
                filled = leftover + source.Read(buffer.AsSpan(leftover));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
