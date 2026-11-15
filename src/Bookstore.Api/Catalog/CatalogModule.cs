using Bookstore.Data;
using Bookstore.Inventory;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Catalog;

public sealed record BookSummary(string Isbn, string Title, string Author, string Category, decimal Price, int PublishedYear);

public sealed record BookDetails(
    string Isbn, string Title, string Author, string Category, decimal Price, int PublishedYear, string Description, int Available, string? Isbn10);

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
            book.Isbn, book.Title, book.Author, book.Category, book.Price, book.PublishedYear, book.Description, available,
            Isbn.ToIsbn10(book.Isbn)));
    }

    private static async Task<Ok<List<CategoryView>>> GetCategories(BookstoreContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryView(c.Name, c.Slug))
            .ToListAsync(ct));
}
