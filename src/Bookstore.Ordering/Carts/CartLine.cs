namespace Bookstore.Ordering.Carts;

public sealed record CartLine(string Isbn, string Title, decimal UnitPrice, int Quantity, bool IsEbook)
{
    public decimal LineTotal => UnitPrice * Quantity;
}
