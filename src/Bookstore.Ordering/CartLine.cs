namespace Bookstore.Ordering;

public sealed record CartLine(string Isbn, string Title, decimal UnitPrice, int Quantity, bool IsEbook);

public sealed record CheckoutTotals(decimal Subtotal, decimal Vat, decimal Shipping, decimal Total);
