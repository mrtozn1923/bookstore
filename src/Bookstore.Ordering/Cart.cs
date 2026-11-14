namespace Bookstore.Ordering;

public sealed record CartLine(string Isbn, string Title, decimal UnitPrice, int Quantity, double WeightKg)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed class Cart
{
    private readonly List<CartLine> lines = [];

    public IReadOnlyList<CartLine> Lines => lines;

    public decimal Subtotal => lines.Sum(l => l.LineTotal);

    public double WeightKg => lines.Sum(l => l.WeightKg * l.Quantity);

    public Cart Add(string isbn, string title, decimal unitPrice, int quantity = 1, double weightKg = 0.4)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        int index = lines.FindIndex(l => l.Isbn == isbn);
        if (index >= 0)
        {
            lines[index] = lines[index] with { Quantity = lines[index].Quantity + quantity };
        }
        else
        {
            lines.Add(new CartLine(isbn, title, unitPrice, quantity, weightKg));
        }

        return this;
    }

    public CartLine? Find(string isbn) => lines.Find(l => l.Isbn == isbn);

    public decimal Total(decimal discountRate) =>
        Math.Round(Subtotal * (1 - discountRate), 2, MidpointRounding.AwayFromZero);
}
