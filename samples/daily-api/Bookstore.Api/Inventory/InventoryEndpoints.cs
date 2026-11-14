using Bookstore.Api.Catalog;

namespace Bookstore.Api.Inventory;

public record StockLine(string Isbn, int Quantity);

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder routes)
    {
        // Depodan gelen toplu stok dosyası; istemci gzip'le sıkıştırıp gönderiyor
        routes.MapPost("/api/inventory/stock-import", (List<StockLine> lines) =>
            TypedResults.Ok(new { Imported = lines.Count, TotalCopies = lines.Sum(l => l.Quantity) }))
            .RequireAuthorization(policy => policy.RequireRole("StoreStaff"));

        // Ayrı bir inventory service'ini temsil ediyor; gelen correlation header'ını logluyor
        routes.MapGet("/internal/inventory/{isbn}", (string isbn, HttpRequest request, ILogger<StockLine> logger) =>
        {
            logger.LogInformation("Inventory lookup for {Isbn}, X-Correlation-Id: {CorrelationId}",
                isbn, request.Headers["X-Correlation-Id"].ToString());
            return TypedResults.Ok(new StockLine(isbn, 12));
        }).ExcludeFromDescription();

        routes.MapGet("/api/books/{isbn}/stock", async (Isbn isbn, IHttpClientFactory clients, CancellationToken cancellationToken) =>
        {
            HttpClient inventory = clients.CreateClient("inventory");
            StockLine? stock = await inventory.GetFromJsonAsync<StockLine>($"/internal/inventory/{isbn}", cancellationToken);
            return TypedResults.Ok(stock);
        });

        routes.MapGet("/api/reports/sales", async (CancellationToken cancellationToken) =>
        {
            // Yavaş bir rapor sorgusunu taklit ediyoruz
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return TypedResults.Ok(new { Total = 125_000m });
        }).WithRequestTimeout("reports");
    }
}
