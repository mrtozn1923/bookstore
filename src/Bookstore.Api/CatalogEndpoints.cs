using System.Text;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;
using StackExchange.Redis;

namespace Bookstore.Api;

// Prices in kurus: JSON numbers are doubles, 279.90 * 100 is not 27990
public sealed record CatalogBook(string Isbn, string Title, string Author, int Year, long PriceKurus, int Stock,
    string[] Categories, string Description);

public sealed record CatalogPriceRequest(long PriceKurus);

public sealed record CatalogHit(string Isbn, string Title, long PriceKurus);

public static class CatalogEndpoints
{
    public const string Index = "idx:catalog";

    // FT.CREATE idx:catalog ON JSON PREFIX 1 bookstore:catalog: LANGUAGE turkish SCHEMA ...
    public static async Task EnsureIndexAsync(IDatabase db)
    {
        ISearchCommandsAsync ft = db.FT();
        if ((await ft._ListAsync()).Any(name => name.ToString() == Index))
        {
            return;
        }
        await ft.CreateAsync(Index,
            new FTCreateParams().On(IndexDataType.JSON).Prefix(RedisKeys.CatalogPrefix).Language("turkish"),
            new Schema()
                .AddTextField(new FieldName("$.title", "title"), weight: 2.0)
                .AddTextField(new FieldName("$.author", "author"))
                .AddTextField(new FieldName("$.description", "description"))
                .AddTagField(new FieldName("$.categories[*]", "category"))
                .AddNumericField(new FieldName("$.priceKurus", "price"), sortable: true)
                .AddNumericField(new FieldName("$.year", "year"), sortable: true)
                .AddNumericField(new FieldName("$.stock", "stock")));
    }

    public static IEndpointRouteBuilder MapCatalog(this IEndpointRouteBuilder app)
    {
        app.MapPut("/catalog/books/{isbn}", async (string isbn, CatalogBook book, IConnectionMultiplexer redis) =>
        {
            await redis.GetDatabase().JSON().SetAsync(RedisKeys.CatalogBook(isbn), "$", book with { Isbn = isbn }); // JSON.SET key $ {...}
            return Results.Ok(new { isbn });
        });

        // Partial update: only the price changes, the index is updated in the same command
        app.MapPatch("/catalog/books/{isbn}/price", async (string isbn, CatalogPriceRequest request, IConnectionMultiplexer redis) =>
        {
            bool updated = await redis.GetDatabase().JSON().SetAsync(RedisKeys.CatalogBook(isbn), "$.priceKurus",
                request.PriceKurus, When.Exists);                                                         // JSON.SET key $.priceKurus 27990 XX
            return updated ? Results.Ok(new { isbn, request.PriceKurus }) : Results.NotFound();
        });

        app.MapGet("/catalog/search", async (string? q, string? category, long? maxPrice, int? page, IConnectionMultiplexer redis) =>
        {
            List<string> parts = [];
            if (!string.IsNullOrWhiteSpace(q))
            {
                parts.Add(string.Join(' ', q.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Escape)));
            }
            if (!string.IsNullOrWhiteSpace(category))
            {
                parts.Add($"@category:{{{Escape(category)}}}");
            }
            if (maxPrice is not null)
            {
                parts.Add($"@price:[-inf {maxPrice}]");
            }
            const int pageSize = 10;
            int pageNo = Math.Max(page ?? 1, 1);
            Query query = new Query(parts.Count == 0 ? "*" : string.Join(' ', parts))
                .ReturnFields("title", "price")
                .Limit((pageNo - 1) * pageSize, pageSize);
            if (string.IsNullOrWhiteSpace(q))
            {
                query.SetSortBy("price", ascending: true);                                // without words, relevance means nothing
            }

            SearchResult result = await redis.GetDatabase().FT().SearchAsync(Index, query);
            CatalogHit[] hits = [.. result.Documents.Select(d =>
                new CatalogHit(d.Id[RedisKeys.CatalogPrefix.Length..], (string)d["title"]!, (long)d["price"]))];
            return Results.Ok(new { query = query.QueryString, total = result.TotalResults, page = pageNo, hits });
        });

        // Facet counts: FT.TAGVALS + one FT.SEARCH ... LIMIT 0 0 per category, sent together
        app.MapGet("/catalog/categories", async (IConnectionMultiplexer redis) =>
        {
            ISearchCommandsAsync ft = redis.GetDatabase().FT();
            RedisResult[] values = await ft.TagValsAsync(Index, "category");
            string[] categories = [.. values.Select(v => v.ToString())];
            SearchResult[] counts = await Task.WhenAll(categories.Select(c =>
                ft.SearchAsync(Index, new Query($"@category:{{{Escape(c)}}}").Limit(0, 0))));
            // TAGVALS may still list a value no document has any more (until the index GC runs)
            return Results.Ok(categories.Zip(counts, (c, r) => new { category = c, books = r.TotalResults })
                .Where(x => x.books > 0)
                .OrderByDescending(x => x.books));
        });

        return app;
    }

    // Query syntax characters (-, @, {, |, :, ...) in user input must be escaped, otherwise "science-fiction" is a syntax error
    public static string Escape(string value)
    {
        StringBuilder sb = new(value.Length);
        foreach (char c in value)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                sb.Append('\\');
            }
            sb.Append(c);
        }
        return sb.ToString();
    }
}
