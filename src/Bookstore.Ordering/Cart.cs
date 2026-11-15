namespace Bookstore.Ordering;

public sealed class Cart
{
    public const int MaxLines = 50;

    private readonly List<OrderLine> lines = [];

    public IReadOnlyList<OrderLine> Lines => lines;

    public bool TryAdd(OrderLine line)
    {
        if (lines.Count >= MaxLines)
        {
            return false;
        }

        lines.Add(line);
        return true;
    }
}
