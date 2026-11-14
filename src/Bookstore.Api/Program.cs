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
