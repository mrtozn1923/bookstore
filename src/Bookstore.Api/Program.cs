WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = builder.Build();

Book[] books =
[
    new("9780441172719", "Dune", 14.99m),
    new("9780141439587", "Emma", 9.50m),
    new("9780547928227", "The Hobbit", 12.00m),
];

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapGet("/api/books", () => books);

app.Run();

public record Book(string Isbn, string Title, decimal Price);
