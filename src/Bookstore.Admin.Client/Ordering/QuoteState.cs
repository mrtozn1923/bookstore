using Bookstore.Admin.Client.Contracts;

namespace Bookstore.Admin.Client.Ordering;

public sealed record QuoteLine(string Isbn, string Title, decimal Price, int Quantity)
{
    public decimal Total => Price * Quantity;
}

// The quote being prepared, shared by the components on the quote page.
// Registered as scoped: one per circuit on the server, one per browser tab in WebAssembly.
public sealed class QuoteState
{
    // Institutional orders: at least 20 copies get 10% off (Bookstore's B2B rule).
    public const int InstitutionalMinimum = 20;
    public const decimal InstitutionalDiscountRate = 0.10m;

    private readonly List<QuoteLine> lines = [];

    public event Action? Changed;

    public IReadOnlyList<QuoteLine> Lines => lines;
    public int TotalQuantity => lines.Sum(l => l.Quantity);
    public decimal Subtotal => lines.Sum(l => l.Total);
    public decimal Discount =>
        TotalQuantity >= InstitutionalMinimum ? Math.Round(Subtotal * InstitutionalDiscountRate, 2) : 0m;
    public decimal Total => Subtotal - Discount;

    public void Add(BookSummary book, int quantity)
    {
        int index = lines.FindIndex(l => l.Isbn == book.Isbn);
        if (index >= 0)
        {
            lines[index] = lines[index] with { Quantity = lines[index].Quantity + quantity };
        }
        else
        {
            lines.Add(new QuoteLine(book.Isbn, book.Title, book.Price, quantity));
        }
        Changed?.Invoke();
    }

    public void Remove(string isbn)
    {
        lines.RemoveAll(l => l.Isbn == isbn);
        Changed?.Invoke();
    }
}
