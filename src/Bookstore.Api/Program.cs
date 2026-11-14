WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSystemd();

WebApplication app = builder.Build();

Book[] books =
[
    new("9780441172719", "Dune", 14.99m),
    new("9780141439587", "Emma", 9.50m),
    new("9780547928227", "The Hobbit", 12.00m),
];

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapGet("/api/books", () => books);
app.MapGet("/api/books/{isbn}", (string isbn) =>
{
    Book? book = books.FirstOrDefault(b => b.Isbn == isbn);
    if (book is null)
    {
        app.Logger.LogWarning("Book {Isbn} not found", isbn);
        return Results.NotFound();
    }
    return Results.Ok(book);
});

// Order lookup: the payment provider sometimes times out (simulated)
app.MapGet("/api/orders/{id:int}", async (int id, HttpContext context) =>
{
    string requestId = context.Request.Headers["X-Request-ID"].FirstOrDefault() ?? context.TraceIdentifier;
    await Task.Delay(Random.Shared.Next(20, 120));
    if (Random.Shared.Next(100) < 4)
    {
        app.Logger.LogError("Payment provider timeout for order {OrderId}, request {RequestId}", id, requestId);
        return Results.Problem("Payment provider timeout", statusCode: 503);
    }
    return Results.Ok(new { id, status = "Shipped", total = 24.49m });
});

// Slow report: lets us watch what happens to an in-flight request during shutdown
app.MapGet("/api/reports/sales", async (int seconds) =>
{
    await Task.Delay(TimeSpan.FromSeconds(seconds));
    return Results.Ok(new { report = "sales", seconds });
});

app.Lifetime.ApplicationStarted.Register(() =>
    app.Logger.LogInformation("Bookstore.Api started, PID {Pid}", Environment.ProcessId));
app.Lifetime.ApplicationStopping.Register(() =>
    app.Logger.LogInformation("Stopping: draining in-flight requests"));
app.Lifetime.ApplicationStopped.Register(() =>
    app.Logger.LogInformation("Stopped cleanly"));

app.Run();

public record Book(string Isbn, string Title, decimal Price);
