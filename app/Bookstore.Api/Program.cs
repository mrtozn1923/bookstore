WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = builder.Build();

string environment = app.Configuration["Bookstore:Environment"] ?? "unknown";
string version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

Book[] books =
[
    new("9780441013593", "Dune", 12.50m),
    new("9780141439587", "Emma", 8.99m),
    new("9780547928227", "The Hobbit", 10.99m),
];

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", environment, version }));
app.MapGet("/api/books", () => books);

app.Run();

internal sealed record Book(string Isbn, string Title, decimal Price);
