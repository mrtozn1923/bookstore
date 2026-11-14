using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;

// Steady user traffic for Bookstore.Api, built from the three critical user journeys:
//   search  GET /api/books?search=…  then GET /api/books/{isbn}
//   cart    POST /api/carts, PUT /api/carts/{id}/items/{isbn}
//   buy     POST /api/orders
// One journey in 20 asks for a book that does not exist (404), and a "load balancer" calls GET /health every 5 s.
// Prints one line per minute: requests per step, 5xx answers, calls that got no answer at all and the slowest checkout.
// Usage: dotnet run --project tools/Bookstore.Traffic -- [journeysPerSecond=4] [minutes=10] [baseUrl=http://localhost:5080]
double rate = args.Length > 0 ? double.Parse(args[0]) : 4;
double minutes = args.Length > 1 ? double.Parse(args[1]) : 10;
Uri baseUrl = new(args.Length > 2 ? args[2] : "http://localhost:5080");

using HttpClient http = new() { BaseAddress = baseUrl, Timeout = TimeSpan.FromSeconds(30) };
PagedBooks catalog = await http.GetFromJsonAsync<PagedBooks>("/api/books?pageSize=50")
    ?? throw new InvalidOperationException("Catalog is empty.");
BookRow[] books = [.. catalog.Items];
int expectedOrders = (int)(rate * minutes * 60);
foreach (BookRow book in books)
{
    // Back office stock receipt: enough copies for the whole run.
    (await http.PostAsJsonAsync("/api/inventory/receipts", new { isbn = book.Isbn, quantity = expectedOrders }))
        .EnsureSuccessStatusCode();
}

ConcurrentQueue<(string Step, int Status, double Ms)> window = new();
using CancellationTokenSource stop = new(TimeSpan.FromMinutes(minutes));
List<Task> running = [];

Task health = Task.Run(async () =>
{
    while (!stop.IsCancellationRequested)
    {
        await Call("health", () => http.GetAsync("/health"));
        await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);
    }
});
Task report = Task.Run(async () =>
{
    while (!stop.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), CancellationToken.None);
        Print();
    }
});

using PeriodicTimer timer = new(TimeSpan.FromSeconds(1 / rate));
int journey = 0;
while (!stop.IsCancellationRequested && await timer.WaitForNextTickAsync(CancellationToken.None))
{
    int n = ++journey;
    running.Add(Task.Run(() => Journey(n)));
    running.RemoveAll(t => t.IsCompleted);
}
await Task.WhenAll([.. running, health, report]);

async Task Journey(int n)
{
    BookRow book = books[Random.Shared.Next(books.Length)];
    string word = book.Title.Split(' ')[^1];
    await Call("search", () => http.GetAsync($"/api/books?search={Uri.EscapeDataString(word)}"));
    string isbn = n % 20 == 0 ? "9780000000000" : book.Isbn;
    if (await Call("book", () => http.GetAsync($"/api/books/{isbn}")) != 200)
    {
        return;
    }

    HttpResponseMessage created;
    try
    {
        created = await http.PostAsync("/api/carts", null);
    }
    catch (HttpRequestException)
    {
        window.Enqueue(("cart", 0, 0));
        return;
    }
    using HttpResponseMessage _ = created;
    if (!created.IsSuccessStatusCode)
    {
        return;
    }
    Cart cart = (await created.Content.ReadFromJsonAsync<Cart>())!;
    await Call("cart", () => http.PutAsJsonAsync($"/api/carts/{cart.Id}/items/{isbn}", new { quantity = 1 }));
    await Call("checkout", () => http.PostAsJsonAsync("/api/orders",
        new { cartId = cart.Id, customerName = "Grace Hopper", customerEmail = $"grace{n % 50}@example.com" }));
}

async Task<int> Call(string step, Func<Task<HttpResponseMessage>> send)
{
    long start = Stopwatch.GetTimestamp();
    int status;
    try
    {
        using HttpResponseMessage response = await send();
        status = (int)response.StatusCode;
    }
    catch (HttpRequestException)
    {
        status = 0;
    }
    window.Enqueue((step, status, Stopwatch.GetElapsedTime(start).TotalMilliseconds));
    return status;
}

void Print()
{
    List<(string Step, int Status, double Ms)> items = [];
    while (window.TryDequeue(out (string, int, double) item))
    {
        items.Add(item);
    }
    string counts = string.Join(" ", items.GroupBy(i => i.Step).OrderBy(g => g.Key)
        .Select(g => $"{g.Key}={g.Count()}"));
    int errors = items.Count(i => i.Status >= 500);
    int noAnswer = items.Count(i => i.Status == 0);
    int notFound = items.Count(i => i.Status == 404);
    double slowest = items.Where(i => i.Step == "checkout").Select(i => i.Ms).DefaultIfEmpty().Max();
    Console.WriteLine($"{DateTime.UtcNow:HH:mm} UTC {counts} 5xx={errors} no-answer={noAnswer} 404={notFound} slowest-checkout={slowest:F0}ms");
}

internal sealed record BookRow(string Isbn, string Title);
internal sealed record PagedBooks(List<BookRow> Items);
internal sealed record Cart(Guid Id);
