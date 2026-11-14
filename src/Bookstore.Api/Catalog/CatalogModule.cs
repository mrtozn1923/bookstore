using System.Diagnostics.CodeAnalysis;
using Bookstore.Data;
using Bookstore.Inventory;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Catalog;

public sealed record BookSummary(string Isbn, string Title, string Author, string Category, decimal Price, int PublishedYear);

public sealed record BookDetails(
    string Isbn, string Title, string Author, string Category, decimal Price, int PublishedYear, string Description, int Available);

public sealed record CategoryView(string Name, string Slug);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services) =>
        services.AddScoped<ICatalogService, CatalogService>();

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder catalog = app.MapGroup("/api").WithTags("Catalog");
        catalog.MapGet("/books", SearchBooks);
        catalog.MapGet("/books/{isbn}", GetBook);
        catalog.MapGet("/books/featured", GetFeatured);
        catalog.MapGet("/books/{isbn}/similar", GetSimilar);
        catalog.MapGet("/categories", GetCategories);
        return app;
    }

    // GET /api/books?search=austen&category=classics&page=1&pageSize=12
    private static async Task<Ok<PagedResult<BookSummary>>> SearchBooks(
        BookstoreContext db, string? search, string? category, int page = 1, int pageSize = 12, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        IQueryable<Book> query = db.Books.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(b => b.Title.Contains(term) || b.Author.Name.Contains(term) || b.Isbn == term);
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(b => b.Category.Slug == category);
        }

        int total = await query.CountAsync(ct);
        List<BookSummary> items = await query
            .OrderBy(b => b.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookSummary(b.Isbn, b.Title, b.Author.Name, b.Category.Name, b.Price, b.PublishedYear))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResult<BookSummary>(items, total, page, pageSize));
    }

    private static async Task<Results<Ok<BookDetails>, NotFound>> GetBook(
        string isbn, BookstoreContext db, IInventoryService inventory, CancellationToken ct)
    {
        var book = await db.Books.AsNoTracking()
            .Where(b => b.Isbn == isbn)
            .Select(b => new { b.Id, b.Isbn, b.Title, Author = b.Author.Name, Category = b.Category.Name, b.Price, b.PublishedYear, b.Description })
            .FirstOrDefaultAsync(ct);
        if (book is null)
        {
            return TypedResults.NotFound();
        }

        int available = await inventory.GetAvailableAsync(book.Id, ct);
        return TypedResults.Ok(new BookDetails(
            book.Isbn, book.Title, book.Author, book.Category, book.Price, book.PublishedYear, book.Description, available));
    }

    // Four random books for the home page; a different selection on every request.
    [SuppressMessage("Security", "CA5394:Do not use insecure randomness",
        Justification = "Picks books to show on the home page; the result is public and grants nothing.")]
    private static async Task<Ok<BookSummary[]>> GetFeatured(BookstoreContext db, CancellationToken ct)
    {
        BookSummary[] books = await db.Books.AsNoTracking()
            .Select(b => new BookSummary(b.Isbn, b.Title, b.Author.Name, b.Category.Name, b.Price, b.PublishedYear))
            .ToArrayAsync(ct);
        Random.Shared.Shuffle(books);
        return TypedResults.Ok(books[..Math.Min(4, books.Length)]);
    }

    // Up to four in-stock books by the same author or in the same category.
    private static async Task<Results<Ok<List<BookSummary>>, NotFound>> GetSimilar(
        string isbn, BookstoreContext db, IInventoryService inventory, CancellationToken ct)
    {
        var book = await db.Books.AsNoTracking()
            .Where(b => b.Isbn == isbn)
            .Select(b => new { b.Id, b.AuthorId, b.CategoryId })
            .FirstOrDefaultAsync(ct);
        if (book is null)
        {
            return TypedResults.NotFound();
        }

        List<Book> candidates = await db.Books.AsNoTracking()
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(b => b.Id != book.Id && (b.AuthorId == book.AuthorId || b.CategoryId == book.CategoryId))
            .OrderBy(b => b.AuthorId == book.AuthorId ? 0 : 1)
            .ThenBy(b => b.Title)
            .ToListAsync(ct);

        List<BookSummary> similar = [];
        foreach (Book candidate in candidates)
        {
            if (similar.Count == 4)
            {
                break;
            }
            if (await inventory.GetAvailableAsync(candidate.Id, ct) > 0)
            {
                similar.Add(new BookSummary(candidate.Isbn, candidate.Title, candidate.Author.Name,
                    candidate.Category.Name, candidate.Price, candidate.PublishedYear));
            }
        }
        return TypedResults.Ok(similar);
    }

    private static async Task<Ok<List<CategoryView>>> GetCategories(BookstoreContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryView(c.Name, c.Slug))
            .ToListAsync(ct));
}
