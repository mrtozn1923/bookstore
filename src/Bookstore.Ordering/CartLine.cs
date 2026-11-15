namespace Bookstore.Ordering;

public sealed record CartLine(string Isbn, string Title, decimal UnitPrice, int Quantity, bool IsEbook)
{
    public decimal UnitPrice { get; } = UnitPrice >= 0
        ? UnitPrice
        : throw new ArgumentOutOfRangeException(nameof(UnitPrice), UnitPrice, "A price cannot be negative.");
}

public sealed record CheckoutTotals(decimal Subtotal, decimal Vat, decimal Shipping, decimal Total);
