namespace Bookstore.Ordering;

public sealed record DateRange(DateOnly From, DateOnly To)
{
    public bool Contains(DateOnly date) => date >= From && date <= To;
}
