using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// usage: dotnet run --project tools/Bookstore.LoadProbe -- <url> [total=300] [concurrency=20] [header=X-Catalog-Instance]
string url = args.Length > 0 ? args[0] : "http://localhost:5300/api/v1/books";
int total = args.Length > 1 ? int.Parse(args[1]) : 300;
int concurrency = args.Length > 2 ? int.Parse(args[2]) : 20;
string header = args.Length > 3 ? args[3] : "X-Catalog-Instance";

using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(30) };
ConcurrentDictionary<string, int> byInstance = new();
ConcurrentDictionary<int, int> byStatus = new();
ConcurrentBag<double> latencies = [];
int next = 0;

Stopwatch elapsed = Stopwatch.StartNew();
await Parallel.ForEachAsync(Enumerable.Range(0, concurrency), async (_, ct) =>
{
    while (Interlocked.Increment(ref next) <= total)
    {
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            using HttpResponseMessage response = await client.GetAsync(url, ct);
            string instance = response.Headers.TryGetValues(header, out IEnumerable<string>? values)
                ? values.First()
                : "(none)";
            byInstance.AddOrUpdate(instance, 1, (_, n) => n + 1);
            byStatus.AddOrUpdate((int)response.StatusCode, 1, (_, n) => n + 1);
        }
        catch (HttpRequestException)
        {
            byStatus.AddOrUpdate(0, 1, (_, n) => n + 1);
        }
        latencies.Add(sw.Elapsed.TotalMilliseconds);
    }
});
elapsed.Stop();

double[] sorted = [.. latencies.Order()];
Console.WriteLine($"{total} requests, concurrency {concurrency}, {elapsed.Elapsed.TotalSeconds:F1} s");
Console.WriteLine($"p50 {sorted[sorted.Length / 2]:F0} ms, p95 {sorted[(int)(sorted.Length * 0.95)]:F0} ms");
foreach ((string instance, int count) in byInstance.OrderBy(p => p.Key))
{
    Console.WriteLine($"  {instance,-12} {count,5}");
}
foreach ((int status, int count) in byStatus.OrderBy(p => p.Key))
{
    Console.WriteLine($"  status {(status == 0 ? "error" : status.ToString()),-5} {count,5}");
}
