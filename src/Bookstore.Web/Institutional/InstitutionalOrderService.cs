using System.Collections.Concurrent;
using Bookstore.Web.Catalog;

namespace Bookstore.Web.Institutional;

public sealed record InstitutionalOrderSummary(IReadOnlyList<(Book Book, int Quantity)> Lines, decimal Subtotal, decimal Discount)
{
    public decimal Total => Subtotal - Discount;
}

public sealed class InstitutionalOrderService(IBookCatalog catalog)
{
    public const int MinimumCopies = 20;
    public const decimal DiscountRate = 0.10m;

    private static readonly ConcurrentDictionary<string, InstitutionalOrderDraft> Submitted = new();
    private static int lastNumber = 1000;

    public async Task<InstitutionalOrderSummary> SummarizeAsync(InstitutionalOrderDraft draft, CancellationToken cancellationToken)
    {
        List<(Book Book, int Quantity)> lines = [];
        foreach ((int bookId, int quantity) in draft.Quantities.Where(q => q.Value > 0))
        {
            if (await catalog.FindAsync(bookId, cancellationToken) is { } book)
            {
                lines.Add((book, quantity));
            }
        }

        decimal subtotal = lines.Sum(l => l.Book.Price * l.Quantity);
        return new InstitutionalOrderSummary(lines, subtotal, Math.Round(subtotal * DiscountRate, 2));
    }

    public string Submit(InstitutionalOrderDraft draft)
    {
        // Stand-in for Bookstore.Institutional: the order waits for PurchaseApproval
        string number = $"IO-{Interlocked.Increment(ref lastNumber)}";
        Submitted[number] = draft;
        return number;
    }
}
