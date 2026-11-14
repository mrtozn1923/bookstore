namespace Bookstore.Data;

public static class BookstoreFunctions
{
    // Mapped to dbo.fn_DiscountedPrice; only usable inside LINQ queries.
    public static decimal DiscountedPrice(decimal price, int percent)
        => throw new NotSupportedException("Only for use in LINQ queries.");
}
