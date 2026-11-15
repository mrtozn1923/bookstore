using Bookstore.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Catalog;

public sealed record ReadingListOrder(string[] Isbns);

// Demo: one reading list for the demo customer, kept in memory until Identity has real customers.
public sealed class ReadingListStore
{
    private readonly Lock gate = new();
    private string[] isbns = [];
    private bool seeded;

    public string[] Get(Func<string[]> seed)
    {
        lock (gate)
        {
            if (!seeded)
            {
                isbns = seed();
                seeded = true;
            }
            return [.. isbns];
        }
    }

    public void Set(string[] order)
    {
        lock (gate)
        {
            isbns = [.. order];
            seeded = true;
        }
    }
}

public static class ReadingListModule
{
    public static IServiceCollection AddReadingList(this IServiceCollection services) =>
        services.AddSingleton<ReadingListStore>();

    public static IEndpointRouteBuilder MapReadingListEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/reading-list").WithTags("Reading list");
        group.MapGet("/", GetList);
        group.MapPut("/order", SaveOrder);
        return app;
    }

    // GET /api/reading-list: the books in the order the customer chose
    private static async Task<Ok<List<BookSummary>>> GetList(BookstoreContext db, ReadingListStore store, CancellationToken ct)
    {
        List<BookSummary> books = await db.Books.AsNoTracking()
            .OrderBy(b => b.Title)
            .Select(b => new BookSummary(b.Isbn, b.Title, b.Author.Name, b.Category.Name, b.Price, b.PublishedYear))
            .ToListAsync(ct);
        string[] order = store.Get(() => [.. books.Take(6).Select(b => b.Isbn)]);
        Dictionary<string, BookSummary> byIsbn = books.ToDictionary(b => b.Isbn);
        return TypedResults.Ok(order.Where(byIsbn.ContainsKey).Select(isbn => byIsbn[isbn]).ToList());
    }

    // PUT /api/reading-list/order {"isbns": [...]}: only a new order of the same books is accepted
    private static Results<NoContent, ValidationProblem> SaveOrder(ReadingListOrder body, ReadingListStore store)
    {
        string[] current = store.Get(() => []);
        if (body.Isbns.Length != current.Length || body.Isbns.Distinct().Count() != body.Isbns.Length
            || !body.Isbns.All(current.Contains))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["isbns"] = ["The order must contain every book of the reading list exactly once."]
            });
        }
        store.Set(body.Isbns);
        return TypedResults.NoContent();
    }
}
