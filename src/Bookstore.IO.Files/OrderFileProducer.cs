using System.Text.Json;
using Bookstore.Ordering;

namespace Bookstore.IO.Files;

// Simulates the ERP export that drops daily order files into the worker's inbox.
public static class OrderFileProducer
{
    public static async Task DropAsync(string inbox, string mode)
    {
        Directory.CreateDirectory(inbox);
        var orders = SampleOrders.Generate(10_000).ToList();

        if (mode == "slow")
        {
            // Writes the final file name directly, in five pieces.
            string path = Path.Join(inbox, "orders-2026-11-14.jsonl");
            await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            await using var writer = new StreamWriter(stream);
            foreach (var chunk in orders.Chunk(2_000))
            {
                foreach (var order in chunk) await writer.WriteLineAsync(JsonSerializer.Serialize(order));
                await writer.FlushAsync();
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} producer: wrote {chunk.Length} orders");
                await Task.Delay(400);
            }
        }
        else
        {
            // Writes under a name the watcher ignores, then renames in one step.
            string temp = Path.Join(inbox, ".orders-2026-11-15.jsonl.tmp");
            await File.WriteAllLinesAsync(temp, orders.Select(o => JsonSerializer.Serialize(o)));
            File.Move(temp, Path.Join(inbox, "orders-2026-11-15.jsonl"));
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} producer: renamed into place");
        }
        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} producer: done");
    }
}
