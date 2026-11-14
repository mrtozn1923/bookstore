using System.Diagnostics;
using System.Net.Http.Json;

// Checkout traffic for Bookstore.Api: every order is a new cart with 1–3 books (every tenth order is an
// institutional one with 12 books), then POST /api/orders. Prints a duration summary of the order calls.
// Usage: dotnet run --project tools/Bookstore.Traffic -- [orders=200] [concurrency=4] [baseUrl=http://localhost:5080]
int orders = args.Length > 0 ? int.Parse(args[0]) : 200;
int concurrency = args.Length > 1 ? int.Parse(args[1]) : 4;
Uri baseUrl = new(args.Length > 2 ? args[2] : "http://localhost:5080");

using HttpClient http = new() { BaseAddress = baseUrl, Timeout = TimeSpan.FromSeconds(30) };

PagedBooks catalog = await http.GetFromJsonAsync<PagedBooks>("/api/books?pageSize=50")
    ?? throw new InvalidOperationException("Catalog is empty.");
string[] isbns = [.. catalog.Items.Select(b => b.Isbn).Where(i => i is not "9780060934347" and not "9781400079988")];

// Enough copies for the whole run (back office stock receipt).
foreach (string isbn in isbns)
{
    (await http.PostAsJsonAsync("/api/inventory/receipts", new { isbn, quantity = orders })).EnsureSuccessStatusCode();
}

List<(int Status, double Ms)> results = [];
Lock gate = new();
await Parallel.ForEachAsync(Enumerable.Range(1, orders), new ParallelOptions { MaxDegreeOfParallelism = concurrency },
    async (n, ct) =>
    {
        string[] books = n % 10 == 0
            ? [.. isbns.OrderBy(_ => Random.Shared.Next()).Take(12)]
            : [.. isbns.OrderBy(_ => Random.Shared.Next()).Take(Random.Shared.Next(1, 4))];

        HttpResponseMessage created = await http.PostAsync("/api/carts", null, ct);
        Cart cart = (await created.Content.ReadFromJsonAsync<Cart>(ct))!;
        foreach (string isbn in books)
        {
            (await http.PutAsJsonAsync($"/api/carts/{cart.Id}/items/{isbn}", new { quantity = 1 }, ct)).EnsureSuccessStatusCode();
        }

        long start = Stopwatch.GetTimestamp();
        HttpResponseMessage response = await http.PostAsJsonAsync("/api/orders",
            new { cartId = cart.Id, customerName = "Grace Hopper", customerEmail = $"grace{n % 50}@example.com" }, ct);
        double ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        lock (gate)
        {
            results.Add(((int)response.StatusCode, ms));
        }
    });

double[] sorted = [.. results.Select(r => r.Ms).Order()];
double P(double q) => sorted[(int)Math.Ceiling(q * sorted.Length) - 1];
Console.WriteLine($"orders={results.Count} concurrency={concurrency}");
Console.WriteLine($"status: {string.Join(", ", results.GroupBy(r => r.Status).OrderBy(g => g.Key).Select(g => $"{g.Key}={g.Count()}"))}");
Console.WriteLine($"POST /api/orders ms: avg={sorted.Average():F0} p50={P(0.50):F0} p95={P(0.95):F0} p99={P(0.99):F0} max={sorted[^1]:F0}");
Console.WriteLine($"slower than 2 s: {sorted.Count(ms => ms > 2000)}, slower than 7 s: {sorted.Count(ms => ms > 7000)}");

internal sealed record BookRow(string Isbn);
internal sealed record PagedBooks(List<BookRow> Items);
internal sealed record Cart(Guid Id);
