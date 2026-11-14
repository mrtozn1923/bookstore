using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Bookstore.Api.Idempotency;
using Microsoft.EntityFrameworkCore;

string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:5319");
builder.Services.AddDbContext<BookstoreContext>(o => o.UseSqlServer(connectionString));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IDistributorGateway, FakeDistributorGateway>();
builder.Services.AddProblemDetails();

WebApplication app = builder.Build();

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    BookstoreContext setup = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
    await setup.Database.EnsureDeletedAsync();
    await setup.Database.EnsureCreatedAsync();
}

// Demo-only authentication: the user comes from the X-Demo-User header, institution 1 = Sample School
app.Use(async (http, next) =>
{
    string user = http.Request.Headers["X-Demo-User"].FirstOrDefault() ?? "ada";
    http.User = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim(ClaimTypes.Name, user), new Claim("institution_id", "1")], "Demo"));
    await next();
});

app.MapPost("/naive/orders", async (CreateOrderRequest request, BookstoreContext db,
        IDistributorGateway distributor, TimeProvider time, CancellationToken ct) =>
    {
        DateTimeOffset oneMinuteAgo = time.GetUtcNow().AddMinutes(-1);
        bool duplicate = await db.Orders.AnyAsync(o =>
            o.InstitutionUnitId == request.InstitutionUnitId && o.BookId == request.BookId &&
            o.Quantity == request.Quantity && o.CreatedAt > oneMinuteAgo, ct);
        if (duplicate)
        {
            return Results.Conflict();
        }

        await distributor.EnsureInStockAsync(request.BookId, request.Quantity, ct);
        Order order = new()
        {
            InstitutionId = 1, InstitutionUnitId = request.InstitutionUnitId, BookId = request.BookId,
            Quantity = request.Quantity, TotalAmount = request.TotalAmount, CreatedAt = time.GetUtcNow()
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/orders/{order.Id}", new OrderDto(order.Id, order.TotalAmount));
    });

app.MapPost("/orders", async (CreateOrderRequest request, HttpContext http,
        BookstoreContext db, IDistributorGateway distributor, TimeProvider time, CancellationToken ct) =>
    {
        if (request.Quantity <= 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["quantity"] = ["Quantity must be greater than zero."]
            });
        }

        await distributor.EnsureInStockAsync(request.BookId, request.Quantity, ct);

        Order order = new()
        {
            InstitutionId = CurrentUser.From(http).InstitutionId,
            InstitutionUnitId = request.InstitutionUnitId,
            BookId = request.BookId,
            Quantity = request.Quantity,
            TotalAmount = request.TotalAmount,
            CreatedAt = time.GetUtcNow()
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/orders/{order.Id}", new OrderDto(order.Id, order.TotalAmount));
    })
    .AddEndpointFilter<IdempotencyFilter<CreateOrderRequest>>();

await app.StartAsync();

using HttpClient client = new() { BaseAddress = new Uri("http://127.0.0.1:5319") };

async Task Send(string label, string? key, object body, string path = "/orders", string userId = "ada")
{
    using HttpRequestMessage req = new(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
    if (key is not null) req.Headers.TryAddWithoutValidation("Idempotency-Key", key);
    req.Headers.Add("X-Demo-User", userId);
    using HttpResponseMessage res = await client.SendAsync(req);
    string replayed = res.Headers.Contains("Idempotent-Replayed") ? " replayed" : "";
    Console.WriteLine($"{label}: {(int)res.StatusCode}{replayed} {res.Headers.Location} {await res.Content.ReadAsStringAsync()}");
}

// Opening: the naive "same order within a minute" rule
var naiveOrder = new { institutionUnitId = 7, bookId = 1, quantity = 30, totalAmount = 1500.00m };
await Task.WhenAll(Send("naive A", null, naiveOrder, "/naive/orders"),
    Send("naive B", null, naiveOrder, "/naive/orders"));
await Send("naive C", null, naiveOrder, "/naive/orders");

// Idempotency-Key scenarios
string key1 = "\"8e03978e-40d5-43e8-bc93-6894a57f9324\"";
var order1 = new { institutionUnitId = 7, bookId = 2, quantity = 50, totalAmount = 4250.00m };

await Send("no key", null, order1);
await Send("first", key1, order1);
await Send("retry", key1, order1);
await Send("same key, other body", key1, order1 with { quantity = 60, totalAmount = 5100.00m });

string key2 = "\"0b5f1c1e-6d0a-4f53-9a43-2c1f1b7f0d11\"";
var order2 = new { institutionUnitId = 7, bookId = 3, quantity = 20, totalAmount = 980.00m };
await Task.WhenAll(Send("parallel A", key2, order2), Send("parallel B", key2, order2));

string key3 = "\"5d2a9c40-31e7-4f0e-b2a9-77d0e8a1c3f4\"";
await Send("invalid", key3, new { institutionUnitId = 7, bookId = 4, quantity = 0, totalAmount = 0m });
await Send("corrected", key3, new { institutionUnitId = 7, bookId = 4, quantity = 10, totalAmount = 310.00m });

// Lease and user scope
var crashedBody = new { institutionUnitId = 7, bookId = 5, quantity = 1, totalAmount = 75.00m };
byte[] crashedHash = IdempotencyFilter<CreateOrderRequest>.Fingerprint("POST", "/orders",
    new CreateOrderRequest(7, 5, 1, 75.00m));
DateTimeOffset now = DateTimeOffset.UtcNow;
await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
    // test: a crashed request's Started record (lease expired 1 s ago) and one still running
    db.IdempotencyRecords.AddRange(
        new IdempotencyRecord { InstitutionId = 1, UserId = "ada", Key = "c3f0a8d2-9b1e-4c57-8a6d-1f2e3d4c5b6a",
            RequestHash = crashedHash, Status = IdempotencyStatus.Started, LockedUntil = now.AddSeconds(-1), ExpiresAt = now.AddHours(24) },
        new IdempotencyRecord { InstitutionId = 1, UserId = "ada", Key = "d4e1b9c3-0a2f-4d68-9b7e-2a3f4e5d6c7b",
            RequestHash = crashedHash, Status = IdempotencyStatus.Started, LockedUntil = now.AddSeconds(30), ExpiresAt = now.AddHours(24) });
    await db.SaveChangesAsync();
}
await Send("expired lease", "\"c3f0a8d2-9b1e-4c57-8a6d-1f2e3d4c5b6a\"", crashedBody);
await Send("live lease", "\"d4e1b9c3-0a2f-4d68-9b7e-2a3f4e5d6c7b\"", crashedBody);
await Send("other user", key1, order1, userId: "alan");

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
    Console.WriteLine($"Orders in DB: {await db.Orders.CountAsync()}");
    foreach (IdempotencyRecord r in await db.IdempotencyRecords.OrderBy(r => r.Id).ToListAsync())
    {
        Console.WriteLine($"{r.UserId} {r.Key[..8]} {r.Status} {r.ResponseStatusCode}".TrimEnd());
    }

    int purged = await db.IdempotencyRecords
        .Where(r => r.ExpiresAt < DateTimeOffset.UtcNow.AddDays(2))
        .ExecuteDeleteAsync();
    Console.WriteLine($"Purged (simulated two days later): {purged}");
}

await app.StopAsync();
