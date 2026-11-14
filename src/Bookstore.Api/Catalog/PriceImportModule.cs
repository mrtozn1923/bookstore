using System.Globalization;
using Bookstore.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Catalog;

public sealed record PriceImportResult(int Updated, IReadOnlyList<string> UnknownIsbns);

public static partial class PriceImportModule
{
    public static IEndpointRouteBuilder MapPriceImportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/prices", ImportPrices).WithTags("Catalog");
        return app;
    }

    // Body: one "isbn;price" pair per line, e.g. "9780441172719;459.90"
    private static async Task<Ok<PriceImportResult>> ImportPrices(
        HttpRequest request, BookstoreContext db, ILogger<PriceImportResult> logger, CancellationToken ct)
    {
        using StreamReader reader = new(request.Body);
        string body = await reader.ReadToEndAsync(ct);

        int updated = 0;
        List<string> unknown = [];
        foreach (string line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] parts = line.Split(';');
            if (Isbn.Normalize(parts[0]) is not { } isbn)
            {
                unknown.Add(parts[0]);
                continue;
            }
            decimal price = decimal.Parse(parts[1], CultureInfo.InvariantCulture);

            Book? book = await db.Books.FirstOrDefaultAsync(b => b.Isbn == isbn, ct);
            if (book is null)
            {
                unknown.Add(isbn);
                continue;
            }
            book.Price = price;
            updated++;
        }

        await db.SaveChangesAsync(ct);
        LogImportFinished(logger, updated, unknown.Count);
        return TypedResults.Ok(new PriceImportResult(updated, unknown));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Price import finished: {Updated} updated, {Unknown} unknown")]
    private static partial void LogImportFinished(ILogger logger, int updated, int unknown);
}
