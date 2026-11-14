using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using Bookstore.Ordering;

namespace Bookstore.IO.Json.Streaming;

public static class StreamingDemos
{
    private const string ExportPath = "out/orders-1m.json";
    private static readonly Order Sample = SampleOrders.Generate(1).Single();

    private static readonly JsonSerializerOptions ReflectionOptions = new(JsonSerializerOptions.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    // Runs in a fresh process: the first call pays for building metadata.
    public static void Startup(string mode)
    {
        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        var sw = Stopwatch.StartNew();
        string json = mode == "sourcegen"
            ? JsonSerializer.Serialize(Sample, BookstoreJsonContext.Default.Order)
            : JsonSerializer.Serialize(Sample, ReflectionOptions);
        sw.Stop();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - allocBefore;
        Console.WriteLine($"{mode,-10} first call: {sw.Elapsed.TotalMilliseconds,6:F1} ms, {allocated,9:N0} bytes, {json.Length} chars");
    }

    public static void NoReflection()
    {
        AppContext.SetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", false);
        try
        {
            JsonSerializer.Serialize(Sample);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Reflection: {ex.GetType().Name}: {ex.Message}");
        }
        Console.WriteLine($"Source gen: {JsonSerializer.Serialize(Sample, BookstoreJsonContext.Default.Order)[..60]}...");
    }

    public static async Task ExportAsync(int count)
    {
        Directory.CreateDirectory("out");
        var sw = Stopwatch.StartNew();
        await using (var file = new FileStream(ExportPath, FileMode.Create, FileAccess.Write, FileShare.None, 0, FileOptions.Asynchronous))
        {
            await OrderExportJson.WriteAsync(file, Generate(count));
        }
        Console.WriteLine($"Wrote {count:N0} orders, {new FileInfo(ExportPath).Length:N0} bytes in {sw.ElapsedMilliseconds:N0} ms, " +
                          $"peak heap {PeakHeap():N0} bytes");

        static async IAsyncEnumerable<Order> Generate(int count)
        {
            foreach (var order in SampleOrders.Generate(count))
            {
                yield return order;
            }
            await Task.CompletedTask;
        }
    }

    public static async Task SummaryAsync(string mode)
    {
        var sw = Stopwatch.StartNew();
        await using var file = new FileStream(ExportPath, FileMode.Open, FileAccess.Read, FileShare.Read, 0, FileOptions.Asynchronous);
        string result;
        if (mode == "list")
        {
            List<Order> all = (await JsonSerializer.DeserializeAsync(file, BookstoreJsonContext.Default.ListOrder))!;
            result = $"{all.Count:N0} orders, {all.Sum(o => o.Total):N2} TRY";
        }
        else if (mode == "stream")
        {
            ExportSummary summary = await OrderExportJson.SummarizeAsync(file);
            result = $"{summary.Orders:N0} orders, {summary.Revenue:N2} TRY, {summary.Customers} customers";
        }
        else
        {
            result = $"{OrderExportJson.SumLineTotals(file):N2} TRY";
        }
        Console.WriteLine($"{mode,-7} {result} in {sw.ElapsedMilliseconds:N0} ms, peak heap {PeakHeap():N0} bytes, " +
                          $"allocated {GC.GetTotalAllocatedBytes():N0} bytes");
    }

    private static long PeakHeap()
    {
        // Largest heap size seen by any GC so far in this process.
        long peak = 0;
        foreach (GCKind kind in new[] { GCKind.Ephemeral, GCKind.FullBlocking, GCKind.Background })
        {
            peak = Math.Max(peak, GC.GetGCMemoryInfo(kind).HeapSizeBytes);
        }
        return Math.Max(peak, GC.GetTotalMemory(false));
    }

    public static void Dom()
    {
        const string webhook = """{"event":"payment.captured","orderNumber":"BS-0000042","amount":"699.80 TRY","attempt":1}""";

        JsonElement kept;
        using (JsonDocument doc = JsonDocument.Parse(webhook))
        {
            JsonElement root = doc.RootElement;
            Console.WriteLine($"JsonDocument: {root.GetProperty("event").GetString()} for {root.GetProperty("orderNumber").GetString()}");
            kept = root;
        }
        try
        {
            Console.WriteLine(kept.GetProperty("event").GetString());
        }
        catch (ObjectDisposedException ex)
        {
            Console.WriteLine($"After Dispose: {ex.GetType().Name}");
        }

        JsonNode node = JsonNode.Parse(webhook)!;
        node["attempt"] = node["attempt"]!.GetValue<int>() + 1;
        node["receivedAtUtc"] = "2026-11-14T09:31:00Z";
        node.AsObject().Remove("amount");
        Console.WriteLine($"JsonNode: {node.ToJsonString()}");
    }

    public static void Schema()
    {
        JsonNode schema = JsonSchemaExporter.GetJsonSchemaAsNode(JsonSerializerOptions.Web, typeof(OrderItem));
        Console.WriteLine(schema.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    public static void Lenient()
    {
        const string legacy = """
            {
              // exported by the old ERP
              "isbn": "9780441172719",
              "title": "Dune",
              "quantity": 2,
              "unitPrice": 349.90,
            }
            """;
        try
        {
            JsonSerializer.Deserialize<OrderItem>(legacy, JsonSerializerOptions.Web);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Web: {ex.Message}");
        }
        var lenient = new JsonSerializerOptions(JsonSerializerOptions.Web)
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        Console.WriteLine($"Lenient: {JsonSerializer.Deserialize<OrderItem>(legacy, lenient)}");
    }
}
