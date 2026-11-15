namespace Bookstore.Admin.Components.Pages.Diagnostics;

// Synthetic rows for measuring list rendering; not real warehouse data.
public sealed record StockMovement(int Id, string Isbn, int Change, DateTime AtUtc)
{
    private static readonly string[] Isbns = ["9780441172719", "9780261102217", "9780141439587", "9780141439549", "9780451524935"];

    public static List<StockMovement> Generate(int count)
    {
        Random random = new(42);
        DateTime start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return [.. Enumerable.Range(1, count).Select(i =>
            new StockMovement(i, Isbns[random.Next(Isbns.Length)], random.Next(-5, 20), start.AddMinutes(i * 7)))];
    }
}
