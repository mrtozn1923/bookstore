using System.ComponentModel.DataAnnotations;
using Bookstore.Data;
using Bookstore.Inventory;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bookstore.Catalog;

public sealed record AuthorView(int Id, string Name, int BookCount);

public sealed record NewBookRequest(
    [property: Required, RegularExpression(@"^\d{13}$", ErrorMessage = "ISBN must be 13 digits.")] string Isbn,
    [property: Required, StringLength(200)] string Title,
    [property: Range(typeof(decimal), "1", "10000")] decimal Price,
    [property: Range(1450, 2100)] int PublishedYear,
    [property: Required] string CategorySlug);

public sealed record NewAuthorRequest(
    [property: Required, StringLength(200)] string Name,
    [property: MinLength(1)] IReadOnlyList<NewBookRequest> Books);

// Back-office endpoints for the catalog, used by Bookstore.Admin. Authorization comes later in the Blazor series.
public static class AuthorEndpoints
{
    public static IEndpointRouteBuilder MapAuthorEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder authors = app.MapGroup("/api/authors").WithTags("Catalog");
        authors.MapGet("/", GetAuthors);
        authors.MapPost("/", CreateAuthor);
        return app;
    }

    private static async Task<Ok<List<AuthorView>>> GetAuthors(BookstoreContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.Authors.AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new AuthorView(a.Id, a.Name, a.Books.Count))
            .ToListAsync(ct));

    // POST /api/authors { "name": "...", "books": [ { "isbn": "...", ... } ] }: a new author with their books.
    private static async Task<Results<Created<AuthorView>, ValidationProblem>> CreateAuthor(
        NewAuthorRequest request, BookstoreContext db, IInventoryService inventory, CancellationToken ct)
    {
        string[] isbns = [.. request.Books.Select(b => b.Isbn)];
        HashSet<string> taken = [.. await db.Books.Where(b => isbns.Contains(b.Isbn)).Select(b => b.Isbn).ToListAsync(ct)];
        Dictionary<string, int> categories = await db.Categories.ToDictionaryAsync(c => c.Slug, c => c.Id, ct);

        Dictionary<string, string[]> errors = [];
        for (int i = 0; i < request.Books.Count; i++)
        {
            NewBookRequest book = request.Books[i];
            if (taken.Contains(book.Isbn))
            {
                errors[$"Books[{i}].Isbn"] = [$"A book with ISBN {book.Isbn} already exists."];
            }
            if (!categories.ContainsKey(book.CategorySlug))
            {
                errors[$"Books[{i}].CategorySlug"] = [$"Unknown category '{book.CategorySlug}'."];
            }
        }
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        Author author = new() { Name = request.Name.Trim() };
        foreach (NewBookRequest book in request.Books)
        {
            author.Books.Add(new Book
            {
                Isbn = book.Isbn,
                Title = book.Title.Trim(),
                Price = book.Price,
                PublishedYear = book.PublishedYear,
                CategoryId = categories[book.CategorySlug]
            });
        }

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(ct);
        db.Authors.Add(author);
        await db.SaveChangesAsync(ct);
        await inventory.StartTrackingAsync([.. author.Books.Select(b => b.Id)], ct);
        await transaction.CommitAsync(ct);

        return TypedResults.Created($"/api/authors/{author.Id}", new AuthorView(author.Id, author.Name, author.Books.Count));
    }
}
