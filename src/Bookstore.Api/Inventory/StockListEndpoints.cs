using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Inventory;

public sealed record StockRow(int Id, string Isbn, string Title, string Warehouse, int OnHand, int Reserved);

// Demo: a back office stock list large enough for virtual scrolling (5,000 rows), generated deterministically.
public static class StockListEndpoints
{
    private static readonly string[] Titles =
        ["Dune", "The Hobbit", "Emma", "Middlemarch", "Pride and Prejudice", "1984", "Brave New World", "Frankenstein",
         "Moby-Dick", "Jane Eyre", "Wuthering Heights", "Great Expectations", "Crime and Punishment", "Anna Karenina",
         "War and Peace", "The Great Gatsby", "Foundation", "The Left Hand of Darkness", "The Count of Monte Cristo", "Don Quixote"];
    private static readonly string[] Editions = ["Hardcover", "Paperback", "Pocket", "Illustrated", "Annotated"];
    private static readonly string[] Warehouses = ["Istanbul", "Ankara", "Izmir"];

    private static readonly Lazy<StockRow[]> Rows = new(() =>
    {
        Random random = new(2026);
        return [.. Enumerable.Range(1, 5000).Select(i =>
        {
            int onHand = random.Next(0, 400);
            return new StockRow(
                i,
                $"978{6050000000 + i:D10}",
                $"{Titles[i % Titles.Length]} · {Editions[i / Titles.Length % Editions.Length]} #{i}",
                Warehouses[random.Next(Warehouses.Length)],
                onHand,
                onHand == 0 ? 0 : random.Next(0, Math.Min(onHand, 40)));
        })];
    });

    public static IEndpointRouteBuilder MapStockListEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/stock", GetStock).WithTags("Inventory");
        return app;
    }

    // GET /api/admin/stock?warehouse=Izmir: the whole list at once; the client virtualizes the rendering
    private static Ok<StockRow[]> GetStock(string? warehouse) =>
        TypedResults.Ok(string.IsNullOrEmpty(warehouse) ? Rows.Value : [.. Rows.Value.Where(r => r.Warehouse == warehouse)]);
}
