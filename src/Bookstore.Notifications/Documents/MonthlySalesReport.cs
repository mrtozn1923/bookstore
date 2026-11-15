namespace Bookstore.Notifications.Documents;

public sealed record SalesRow(int OrderId, DateTime PlacedAtUtc, string CustomerEmail, string Isbn, string Title, int Quantity, decimal UnitPrice)
{
    public decimal Total => Quantity * UnitPrice;
}

public sealed record MonthlySalesReport(DateOnly Month, IReadOnlyList<SalesRow> Rows);
