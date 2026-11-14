using System.Net;
using System.Net.Http.Headers;
using Bookstore.Gateway.Traffic;
using Microsoft.Net.Http.Headers;

namespace Bookstore.Gateway.Bff;

public static class BookDetailsEndpoint
{
    // Stock and purchase history are nice to have; the page does not wait longer than this for them.
    private static readonly TimeSpan OptionalBudget = TimeSpan.FromMilliseconds(800);

    public static IEndpointRouteBuilder MapBookDetails(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bff/books/{isbn}", GetAsync)
            .AllowAnonymous()
            .WithRequestTimeout(TrafficPolicies.CatalogRead);
        return app;
    }

    private static async Task<IResult> GetAsync(
        string isbn, HttpContext context, IHttpClientFactory http, ILoggerFactory loggers, CancellationToken ct)
    {
        using CancellationTokenSource optional = CancellationTokenSource.CreateLinkedTokenSource(ct);
        optional.CancelAfter(OptionalBudget);

        // Token passthrough: Ordering validates the same token itself (post 04).
        AuthenticationHeaderValue? bearer =
            AuthenticationHeaderValue.TryParse(context.Request.Headers[HeaderNames.Authorization], out AuthenticationHeaderValue? parsed)
                ? parsed
                : null;
        bool signedIn = context.User.Identity?.IsAuthenticated == true;

        Task<DownstreamResult<CatalogBook>> book = DownstreamCall.GetAsync<CatalogBook>(
            http.CreateClient(BffClients.Catalog), $"/books/{isbn}", null, ct);
        Task<DownstreamResult<StockLevel>> stock = DownstreamCall.GetAsync<StockLevel>(
            http.CreateClient(BffClients.Inventory), $"/stock/{isbn}", null, optional.Token);
        Task<DownstreamResult<Purchase>> purchase = signedIn
            ? DownstreamCall.GetAsync<Purchase>(http.CreateClient(BffClients.Ordering), $"/orders/purchases/{isbn}", bearer, optional.Token)
            : Task.FromResult(new DownstreamResult<Purchase>(default, null, "anonymous", 0));

        await Task.WhenAll(book, stock, purchase);

        loggers.CreateLogger("Bookstore.Gateway.Bff").LogInformation(
            "Book {Isbn}: catalog {CatalogMs} ms {CatalogError}, inventory {InventoryMs} ms {InventoryError}, ordering {OrderingMs} ms {OrderingError}",
            isbn, book.Result.ElapsedMs, book.Result.Error ?? "ok", stock.Result.ElapsedMs, stock.Result.Error ?? "ok",
            purchase.Result.ElapsedMs, purchase.Result.Error ?? "ok");

        // Catalog is required: without the book there is no page.
        if (book.Result.Status == HttpStatusCode.NotFound)
        {
            return Results.NotFound();
        }
        if (book.Result.Value is not { } b)
        {
            return Results.Problem(statusCode: StatusCodes.Status502BadGateway, title: "Catalog is unavailable");
        }

        List<string> warnings = [];
        if (stock.Result.Error is { } stockError)
        {
            warnings.Add($"stock-unavailable: {stockError}");
        }
        if (signedIn && purchase.Result.Error is { } purchaseError)
        {
            warnings.Add($"purchase-history-unavailable: {purchaseError}");
        }

        return Results.Ok(new BookDetailsView(
            b.Isbn, b.Title, b.Author, b.Price,
            stock.Result.Value?.Available,
            purchase.Result.Value?.PurchasedBefore,
            warnings));
    }
}
