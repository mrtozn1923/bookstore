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

app.MapGet("/api/catalog/books", () => books);

app.MapGet("/api/catalog/books/{isbn}", (string isbn) =>
    books.FirstOrDefault(b => b.Isbn == isbn) is { } book
        ? Results.Ok(book)
        : Results.NotFound());

app.Run();
