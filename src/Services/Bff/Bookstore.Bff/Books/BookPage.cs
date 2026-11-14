using System.Net;
using Bookstore.Inventory.V1;
using Grpc.Core;
using Polly.Timeout;

namespace Bookstore.Bff.Books;

// What the Angular book page needs in one call. Stock is optional: the page works without it.
public sealed record BookPageResponse(
    string Isbn, string Title, string Author, string Category, decimal Price, string Currency, int PublishedYear,
    string Description, StockInfo? Stock);

public sealed record StockInfo(string Status, int Available);

// Catalog's response, only the fields the page shows (tolerant reader).
internal sealed record CatalogBook(
    string Isbn, string Title, string Author, string Category, decimal Price, string Currency, int PublishedYear, string Description);

public sealed class CatalogApi(HttpClient http)
{
    internal async Task<CatalogBook?> GetBookAsync(string isbn, CancellationToken ct)
    {
        using HttpResponseMessage response = await http.GetAsync($"/api/v1/books/{Uri.EscapeDataString(isbn)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CatalogBook>(ct);
    }
}

public static class BookPageEndpoint
{
    // Aggregation lives in the BFF because it is shaped by one screen. It contains no business rule:
    // it only combines two answers and decides what the page shows when one of them is missing.
    public static IEndpointRouteBuilder MapBookPage(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/books/{isbn}", GetAsync);
        return app;
    }

    private static async Task<IResult> GetAsync(
        string isbn, CatalogApi catalog, InventoryService.InventoryServiceClient inventory, TimeProvider clock,
        ILogger<CatalogApi> logger, CancellationToken ct)
    {
        // The page's own budget for Catalog. Without it the standard resilience handler waits up to 30 seconds
        // (10-second attempts, retried) before giving up.
        using CancellationTokenSource catalogBudget = CancellationTokenSource.CreateLinkedTokenSource(ct);
        catalogBudget.CancelAfter(TimeSpan.FromSeconds(2));

        // Both calls start at once; the page waits for the slower one, not for the sum.
        Task<CatalogBook?> bookTask = catalog.GetBookAsync(isbn, catalogBudget.Token);
        Task<StockInfo?> stockTask = GetStockAsync(isbn, inventory, clock, logger, ct);

        CatalogBook? book;
        try
        {
            book = await bookTask;
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutRejectedException or OperationCanceledException && !ct.IsCancellationRequested)
        {
            // Without Catalog there is no page to show.
            logger.LogWarning("Catalog failed for {Isbn}: {Error}", isbn, ex.GetType().Name);
            return Results.Problem(title: "The book page is temporarily unavailable", statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        if (book is null)
        {
            return Results.Problem(title: "Book not found", statusCode: StatusCodes.Status404NotFound);
        }

        StockInfo? stock = await stockTask;
        return Results.Ok(new BookPageResponse(
            book.Isbn, book.Title, book.Author, book.Category, book.Price, book.Currency, book.PublishedYear, book.Description, stock));
    }

    private static async Task<StockInfo?> GetStockAsync(
        string isbn, InventoryService.InventoryServiceClient inventory, TimeProvider clock, ILogger logger, CancellationToken ct)
    {
        try
        {
            // A short deadline: a page without stock information is better than a slow page.
            GetStockResponse stock = await inventory.GetStockAsync(
                new GetStockRequest { BookId = isbn },
                deadline: clock.GetUtcNow().UtcDateTime.AddMilliseconds(500),
                cancellationToken: ct);
            return new StockInfo(stock.Status.ToString(), stock.Available);
        }
        catch (RpcException ex)
        {
            // Partial failure: the page is shown, the stock badge says "unknown".
            logger.LogWarning("Stock for {Isbn} unavailable: {Status}", isbn, ex.StatusCode);
            return null;
        }
    }
}
