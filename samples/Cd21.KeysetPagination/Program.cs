using System.Text;
using Bookstore.Ordering.Paging;
using Microsoft.EntityFrameworkCore;

string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";
// demo key; in a real application it comes from configuration / a key store
byte[] cursorKey = Encoding.UTF8.GetBytes("demo-only-cursor-signing-key-32b");

DbContextOptions<BookstoreContext> options = new DbContextOptionsBuilder<BookstoreContext>().UseSqlServer(connectionString).Options;
await using BookstoreContext db = new(options);
CursorCodec codec = new(cursorKey);
OrderPager pager = new(db, codec);
int institutionId = 7;

async Task<int> InsertOrder()
{
    Order order = new()
    {
        InstitutionId = 7, InstitutionUnitId = 1, CreatedAt = new DateTime(2024, 1, 3, 8, 0, 0), TotalAmount = 450m, Status = 0
    };
    db.Orders.Add(order);
    await db.SaveChangesAsync();
    db.ChangeTracker.Clear();
    return order.Id;
}

Task<int> RemoveDemoOrders() => db.Orders.Where(o => o.Id > 2_000_000).ExecuteDeleteAsync();

// 1) Offset: the second page while a new order arrives
async Task<List<int>> Offset(int page) => await db.Orders.AsNoTracking()
    .Where(o => o.InstitutionId == institutionId)
    .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
    .Skip((page - 1) * 5).Take(5)
    .Select(o => o.Id).ToListAsync();

List<int> offset1 = await Offset(1);
int newId = await InsertOrder(); // meanwhile someone opens a new order
List<int> offset2 = await Offset(2);
Console.WriteLine($"page 1: {string.Join(",", offset1)}");
Console.WriteLine($"new order: {newId}");
Console.WriteLine($"page 2: {string.Join(",", offset2)}");
await RemoveDemoOrders();
Console.WriteLine("---");

// 2) Keyset with a signed cursor
CursorPage<OrderRow> p1 = await pager.GetPageAsync(institutionId, null, 5);
newId = await InsertOrder();
CursorPage<OrderRow> p2 = await pager.GetPageAsync(institutionId, p1.Next, 5);
CursorPage<OrderRow> back = await pager.GetPageAsync(institutionId, p2.Previous, 5);
Console.WriteLine($"page 1: {string.Join(",", p1.Items.Select(r => r.Id))}");
Console.WriteLine($"new order: {newId}");
Console.WriteLine($"page 2: {string.Join(",", p2.Items.Select(r => r.Id))}");
Console.WriteLine($"previous of page 2: {string.Join(",", back.Items.Select(r => r.Id))} (prev cursor: {(back.Previous is null ? "no" : "yes")})");
Console.WriteLine($"cursor: {p1.Next}");
await RemoveDemoOrders();
Console.WriteLine("---");

codec.TryDecode(p1.Next!, out OrderCursor? decoded);
Console.WriteLine(pager.BuildQuery(institutionId, decoded).Take(5 + 1)
    .Select(o => new OrderRow(o.Id, o.CreatedAt, o.TotalAmount)).ToQueryString());
Console.WriteLine("---");

// 3) Tampered cursor and another institution's cursor
string tampered = p1.Next!.Replace("Ijo3", "Ijo4");
foreach ((string label, int inst, string token) in new[] { ("tampered", 7, tampered), ("other institution", 8, p1.Next!) })
{
    try
    {
        await pager.GetPageAsync(inst, token, 5);
        Console.WriteLine($"{label}: accepted");
    }
    catch (InvalidCursorException ex)
    {
        Console.WriteLine($"{label}: {ex.Message}");
    }
}
Console.WriteLine("---");

// 4) Nullable sort column: walking approvals by ApprovedAt DESC in a one-hour range
DateTime t0 = new(2024, 1, 2, 0, 0, 0);
IQueryable<Order> approvals = db.Orders.AsNoTracking()
    .Where(o => o.InstitutionId == 7 && o.CreatedAt >= t0 && o.CreatedAt < t0.AddHours(1));

async Task<int> WalkApprovals(bool nullSafe)
{
    int count = 0;
    (DateTime? ApprovedAt, int Id)? last = null;
    while (true)
    {
        IQueryable<Order> q = approvals;
        if (last is { } c)
        {
            q = !nullSafe
                ? q.Where(o => o.ApprovedAt < c.ApprovedAt || (o.ApprovedAt == c.ApprovedAt && o.Id < c.Id))
                : c.ApprovedAt is not null
                    // NULLs sort last: we have not reached the NULL zone yet, the NULLs are still ahead
                    ? q.Where(o => o.ApprovedAt < c.ApprovedAt || (o.ApprovedAt == c.ApprovedAt && o.Id < c.Id) || o.ApprovedAt == null)
                    : q.Where(o => o.ApprovedAt == null && o.Id < c.Id);
        }
        var rows = await q.OrderByDescending(o => o.ApprovedAt).ThenByDescending(o => o.Id)
            .Take(500).Select(o => new { o.ApprovedAt, o.Id }).ToListAsync();
        if (rows.Count == 0)
        {
            return count;
        }
        count += rows.Count;
        last = (rows[^1].ApprovedAt, rows[^1].Id);
    }
}

Console.WriteLine($"naive walk: {await WalkApprovals(false)} rows");
Console.WriteLine($"null-safe walk: {await WalkApprovals(true)} rows");
Console.WriteLine($"COUNT(*): {await approvals.CountAsync()}, of which NULL: {await approvals.CountAsync(o => o.ApprovedAt == null)}");
Console.WriteLine("---");

// 5) The endpoint
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:5321");
builder.Services.AddDbContext<BookstoreContext>(o => o.UseSqlServer(connectionString));
builder.Services.AddSingleton(new CursorCodec(cursorKey));
builder.Services.AddScoped<OrderPager>();
builder.Services.AddProblemDetails();
WebApplication app = builder.Build();

app.MapGet("/orders", async (string? cursor, int? pageSize, OrderPager pager, CancellationToken ct) =>
{
    const int institutionId = 7; // demo: in a real application from the tenant claim
    try
    {
        return Results.Ok(await pager.GetPageAsync(institutionId, cursor, Math.Clamp(pageSize ?? 20, 1, 100), ct));
    }
    catch (InvalidCursorException ex)
    {
        return Results.Problem(statusCode: 400, title: ex.Message);
    }
});

await app.StartAsync();
using HttpClient http = new() { BaseAddress = new Uri("http://127.0.0.1:5321") };
foreach (string url in new[] { "/orders?pageSize=2", "/orders?cursor=abc.def" })
{
    using HttpResponseMessage res = await http.GetAsync(url);
    string status = res.IsSuccessStatusCode ? "" : $"{(int)res.StatusCode} ";
    Console.WriteLine($"GET {url}");
    Console.WriteLine($"{status}{await res.Content.ReadAsStringAsync()}");
}
await app.StopAsync();
