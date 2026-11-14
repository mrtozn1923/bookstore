using Bookstore.Catalog;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

Book[] books =
[
    new("9780441172719", "Dune", "Frank Herbert", 420.00m),
    new("9780141439587", "Emma", "Jane Austen", 185.50m),
    new("9780547928227", "The Hobbit", "J. R. R. Tolkien", 310.00m),
    new("9780141439549", "Middlemarch", "George Eliot", 265.00m),
];

app.MapGet("/books", () => books);

app.MapGet("/books/summary", () =>
    books.Select(b => new BookSummary(b.Isbn, b.Title)));

app.MapGet("/books/{isbn}", (string isbn) =>
    books.FirstOrDefault(b => b.Isbn == isbn) is { } book
        ? Results.Ok(book)
        : Results.NotFound());

if (app.Environment.IsDevelopment())
{
    // Shows what the gateway actually sent: path, query, Host and X-*/Forwarded headers.
    app.MapGet("/debug/request", (HttpRequest request) => new
    {
        Path = request.Path.Value,
        Query = request.QueryString.Value,
        Host = request.Host.Value,
        Headers = request.Headers
            .Where(h => h.Key.StartsWith("X-", StringComparison.OrdinalIgnoreCase)
                || h.Key.Equals("Forwarded", StringComparison.OrdinalIgnoreCase))
            .OrderBy(h => h.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(h => h.Key, h => h.Value.ToString()),
    });
}

app.Run();
