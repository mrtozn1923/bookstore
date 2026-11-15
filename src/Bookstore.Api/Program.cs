using System.Security.Claims;
using Bookstore.Api;
using Bookstore.Data;
using Bookstore.Ordering;
using Bookstore.Privacy;
using Bookstore.Privacy.Retention;
using Bookstore.Privacy.Telemetry;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("This sample uses demo authentication and runs only in Development.");
}

builder.Services.AddDbContext<BookstoreContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Bookstore") ?? "Data Source=bookstore.db"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<DataSubjectRequests>();
builder.Services.AddSingleton<IErasureLedger>(new FileErasureLedger(builder.Configuration["Privacy:ErasureLedgerPath"] ?? "erasure-ledger.txt"));
builder.Services.AddScoped<ErasureProcessor>();
builder.Services.AddScoped<PeriodicDestructionJob>();
builder.Services.AddHostedService<DestructionWorker>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<OrderLookupService>();
builder.Services.AddAuthentication(DemoCustomerAuthHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DemoCustomerAuthHandler>(DemoCustomerAuthHandler.SchemeName, null);
builder.Services.AddAuthorization();

var app = builder.Build();
await SeedAsync(app.Services);

app.UseAuthentication();
app.UseAuthorization();

RouteGroupBuilder me = app.MapGroup("/api/me").RequireAuthorization();

// KVKK md. 11/1-b, GDPR Art. 15: right of access
me.MapGet("/personal-data", async (ClaimsPrincipal user, DataSubjectRequests requests, CancellationToken ct) =>
    await requests.ExportAsync(CustomerId(user), ct) is { } export ? Results.Ok(export) : Results.NotFound());

// KVKK md. 11/1-d, GDPR Art. 16: right to rectification
me.MapPatch("/profile", async (ProfileCorrection change, ClaimsPrincipal user, DataSubjectRequests requests, CancellationToken ct) =>
    await requests.CorrectAsync(CustomerId(user), change, ct) ? Results.NoContent() : Results.NotFound());

// KVKK md. 7 and 11/1-e, GDPR Art. 17: erasure is a tracked request, not a synchronous DELETE
me.MapPost("/erasure-requests", async (ClaimsPrincipal user, DataSubjectRequests requests, CancellationToken ct) =>
{
    ErasureRequest request = await requests.RequestErasureAsync(CustomerId(user), ct);
    return Results.Accepted($"/api/me/erasure-requests/{request.Id}", new { request.Id, request.DueAtUtc });
});

me.MapPost("/checkout", async (CheckoutRequest request, ClaimsPrincipal user, CheckoutService checkout, CancellationToken ct) =>
    Results.Ok(new { OrderId = await checkout.PlaceOrderAsync(CustomerId(user), request, ct) }));

app.MapPost("/api/orders/lookup", async (OrderLookupRequest request, OrderLookupService lookup, CancellationToken ct) =>
    await lookup.FindAsync(request, ct) is { } status ? Results.Ok(status) : Results.NotFound());

// Our own collector for browser errors: we decide what is kept before anything is written.
app.MapPost("/api/client-errors", (ClientErrorReport report, HttpContext http, ILogger<ClientErrorReport> logger) =>
{
    logger.LogWarning("Client error at {Page}: {Message} (client {ClientNetwork})",
        TelemetryScrubber.ScrubUrl(report.Url, ClientErrorReport.AllowedQueryKeys),
        TelemetryScrubber.ScrubText(report.Message),
        TelemetryScrubber.TruncateIp(http.Connection.RemoteIpAddress));
    return Results.Accepted();
});

app.Run();

static int CustomerId(ClaimsPrincipal user) => int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

static async Task SeedAsync(IServiceProvider services)
{
    using IServiceScope scope = services.CreateScope();
    BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();

    var created = new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);
    for (int i = 0; i < FakePeople.All.Length; i++)
    {
        Customer c = FakePeople.ToCustomer(i, created.AddDays(i));
        c.Orders.Add(new Order { OrderedAtUtc = created.AddDays(i + 10), RetainUntilUtc = RetentionPolicy.OrderBillingRetainUntil(created.AddDays(i + 10)), Total = 412.50m, ShippingAddress = $"Example St. {i + 1}, {c.City}", BillingName = c.FullName });
        c.Sessions.Add(new CustomerSession
        {
            SignedInAtUtc = created.AddDays(i + 10).AddMinutes(-5),
            IpAddress = "127.0.0.1",
            DeviceId = $"device-{i + 1:0000}",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
        });
        db.Customers.Add(c);
    }
    await db.SaveChangesAsync();
}
