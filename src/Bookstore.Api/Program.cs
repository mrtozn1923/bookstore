using Bookstore.Api.Catalog;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<BookCatalog>();

var app = builder.Build();

app.MapGet("/api/books", (BookCatalog catalog, string? q) =>
    q is null ? catalog.All() : catalog.Search(q));

app.MapGet("/api/books/{isbn}", (BookCatalog catalog, string isbn) =>
    catalog.Find(isbn) is { } book ? Results.Ok(book) : Results.NotFound());

app.Run();
