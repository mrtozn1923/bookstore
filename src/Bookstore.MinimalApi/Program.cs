using Microsoft.AspNetCore.Mvc;
using Bookstore.MinimalApi.Endpoints;
using Bookstore.MinimalApi.Models;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

List<Book> books = new()
{
    new Book { Id = 1, Title = "Dune", Price = 450m, CategoryId = 1 },
    new Book { Id = 2, Title = "Emma", Price = 280m, CategoryId = 2 },
    new Book { Id = 3, Title = "The Hobbit", Price = 320m, CategoryId = 1 }
};

app.MapGet("/api/books", () => books);

app.MapGet("/api/books/{id:int}", (int id) =>
{
    Book? book = books.FirstOrDefault(b => b.Id == id);
    return book is null ? Results.NotFound() : Results.Ok(book);
})
.WithName("GetBookById");

app.MapPost("/api/books", (Book book) =>
{
    book.Id = books.Max(b => b.Id) + 1;
    books.Add(book);
    return Results.CreatedAtRoute("GetBookById", new { id = book.Id }, book);
});

app.MapPut("/api/books/{id:int}", (int id, Book input) =>
{
    Book? existing = books.FirstOrDefault(b => b.Id == id);
    if (existing is null)
    {
        return Results.NotFound();
    }

    existing.Title = input.Title;
    existing.Price = input.Price;
    existing.CategoryId = input.CategoryId;
    return Results.NoContent();
});

app.MapDelete("/api/books/{id:int}", (int id) =>
{
    Book? existing = books.FirstOrDefault(b => b.Id == id);
    if (existing is null)
    {
        return Results.NotFound();
    }

    books.Remove(existing);
    return Results.NoContent();
});

app.MapGet("/api/info", (
    [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
    IWebHostEnvironment environment,
    HttpContext context) =>
{
    return Results.Ok(new
    {
        correlationId,
        environment = environment.EnvironmentName,
        path = context.Request.Path.Value
    });
});

app.MapGet("/api/hello", () => "Hello from minimal API");

app.MapGet("/api/books/{id:int}/problem", (int id) =>
    Results.Problem(
        title: "Book not found",
        detail: $"There is no book with id {id}.",
        statusCode: StatusCodes.Status404NotFound));

app.MapPost("/api/v2/books", (Book book) =>
{
    Dictionary<string, string[]> errors = ModelValidator.Validate(book);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    book.Id = books.Max(b => b.Id) + 1;
    books.Add(book);
    return Results.CreatedAtRoute("GetBookById", new { id = book.Id }, book);
});

app.MapGet("/api/books/search", (string? title, decimal? minPrice, int page) =>
{
    IEnumerable<Book> result = books;
    if (!string.IsNullOrWhiteSpace(title))
    {
        result = result.Where(b => b.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
    }

    if (minPrice is not null)
    {
        result = result.Where(b => b.Price >= minPrice);
    }

    return result.Skip((Math.Max(page, 1) - 1) * 10).Take(10);
});

app.MapCategoryEndpoints();

app.Run();
