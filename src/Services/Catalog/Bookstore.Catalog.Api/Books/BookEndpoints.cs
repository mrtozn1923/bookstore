using Asp.Versioning;
using Bookstore.Catalog.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace Bookstore.Catalog.Books;

public static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder v1 = app.NewVersionedApi("Catalog")
            .MapGroup("/api/v{version:apiVersion}")
            .HasApiVersion(1.0);

        v1.MapGet("/books", SearchBooks).CacheOutput(CachePolicies.BookLists);
        v1.MapGet("/books/{isbn}", GetBook).CacheOutput(CachePolicies.Book).ProducesProblem(StatusCodes.Status404NotFound);
        v1.MapGet("/categories", GetCategories).CacheOutput(CachePolicies.Categories);

        // Back-office endpoints; authorization arrives in post 10.
        RouteGroupBuilder admin = v1.MapGroup("/admin/books");
        admin.MapPut("/{isbn}/price", ChangePrice).ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        admin.MapPut("/{isbn}/title", RenameBook).ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        return app;
    }

    // GET /api/v1/books?search=austen&category=classics&page=1&pageSize=12
    private static async Task<Ok<PagedResponse<BookSummaryResponse>>> SearchBooks(
        CatalogDbContext db, string? search, string? category, int page = 1, int pageSize = 12, CancellationToken ct = default)
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
        List<BookSummaryResponse> items = await query
            .OrderBy(b => b.Title)
            .ThenBy(b => b.Isbn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookSummaryResponse(
                b.Isbn, b.Title, b.Author.Name, b.Category.Name, b.Price, Data.Catalog.Currency, b.PublishedYear, b.Version))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResponse<BookSummaryResponse>(items, page, pageSize, total));
    }

    private static async Task<Results<Ok<BookResponse>, ProblemHttpResult>> GetBook(
        string isbn, CatalogDbContext db, CancellationToken ct)
    {
        BookResponse? book = await db.Books.AsNoTracking()
            .Where(b => b.Isbn == isbn)
            .Select(b => new BookResponse(
                b.Isbn, b.Title, b.Author.Name, b.Category.Name, b.Price, Data.Catalog.Currency,
                b.PublishedYear, b.Description, b.Version))
            .FirstOrDefaultAsync(ct);

        return book is null ? BookNotFound(isbn) : TypedResults.Ok(book);
    }

    private static async Task<Ok<List<CategoryResponse>>> GetCategories(CatalogDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Name, c.Slug))
            .ToListAsync(ct));

    private static Task<Results<NoContent, ProblemHttpResult>> ChangePrice(
        string isbn, ChangePriceRequest request, IDbContextOutbox<CatalogDbContext> outbox, IOutputCacheStore cache,
        TimeProvider clock, CancellationToken ct) =>
        ChangeBookAsync(isbn, book => book.ChangePrice(request.Price, clock), outbox, cache, ct);

    private static Task<Results<NoContent, ProblemHttpResult>> RenameBook(
        string isbn, RenameBookRequest request, IDbContextOutbox<CatalogDbContext> outbox, IOutputCacheStore cache,
        TimeProvider clock, CancellationToken ct) =>
        ChangeBookAsync(isbn, book => book.Rename(request.Title.Trim(), clock), outbox, cache, ct);

    private static async Task<Results<NoContent, ProblemHttpResult>> ChangeBookAsync(
        string isbn, Action<Book> change, IDbContextOutbox<CatalogDbContext> outbox, IOutputCacheStore cache, CancellationToken ct)
    {
        Book? book = await outbox.DbContext.Books.FirstOrDefaultAsync(b => b.Isbn == isbn, ct);
        if (book is null)
        {
            return BookNotFound(isbn);
        }

        change(book);
        foreach (object integrationEvent in book.Events)
        {
            // Not sent yet: queued in memory, written to the outbox table in the same transaction as the book.
            await outbox.PublishAsync(integrationEvent);
        }
        book.ClearEvents();

        try
        {
            // One transaction: UPDATE Books + INSERT into wolverine_outgoing_envelopes. After the commit,
            // Wolverine sends the messages to RabbitMQ and deletes them from the outbox.
            await outbox.SaveChangesAndFlushMessagesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TypedResults.Problem(
                title: "Book was changed by another request",
                detail: $"Book {isbn} was updated in the meantime. Read it again and retry.",
                statusCode: StatusCodes.Status409Conflict);
        }

        // Both the detail page and every list page that may contain this book are stale now.
        await cache.EvictByTagAsync(CachePolicies.BookTag(isbn), ct);
        await cache.EvictByTagAsync(CachePolicies.BookListsTag, ct);

        return TypedResults.NoContent();
    }

    private static ProblemHttpResult BookNotFound(string isbn) =>
        TypedResults.Problem(
            title: "Book not found",
            detail: $"There is no book with ISBN {isbn}.",
            statusCode: StatusCodes.Status404NotFound);
}
