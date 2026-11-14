using System.Text.Json;
using Bookstore.Ordering;

namespace Bookstore.OrderImport.Worker;

public sealed record ImportResult(int Orders, decimal Total);

public sealed class OrderFileImporter
{
    // Opens the file exclusively: if the producer still has it open, this throws IOException.
    public async Task<ImportResult> ImportAsync(string path, CancellationToken ct)
    {
        await using var stream = new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        });
        using var reader = new StreamReader(stream);

        int count = 0;
        decimal total = 0;
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            var order = JsonSerializer.Deserialize<Order>(line)!;
            count++;
            total += order.Total;
        }
        return new ImportResult(count, total);
    }
}
