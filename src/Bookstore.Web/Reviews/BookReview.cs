namespace Bookstore.Web.Reviews;

public enum ReviewStatus
{
    Pending,
    Approved,
    Rejected,
}

// A star rating with an optional text; Text == null means "rating only"
public sealed record BookReview(
    int Id,
    string Isbn,
    string CustomerId,
    string AuthorName,
    int Rating,
    string? Text,
    DateTimeOffset CreatedAt,
    ReviewStatus Status,
    bool IsIncentivized = false);

public sealed record RatingSummary(string Isbn, int RatingCount, int ReviewCount, double Average)
{
    // Rounded once; the page and the JSON-LD both print this value
    public decimal RatingValue => Math.Round((decimal)Average, 1, MidpointRounding.AwayFromZero);
}

public static class ReviewQueries
{
    // Only moderated reviews count; written as IQueryable so EF Core can run it in SQL
    public static IQueryable<RatingSummary> Summaries(this IQueryable<BookReview> reviews) =>
        reviews
            .Where(r => r.Status == ReviewStatus.Approved)
            .GroupBy(r => r.Isbn)
            .Select(g => new RatingSummary(
                g.Key,
                g.Count(),
                g.Count(r => r.Text != null),
                g.Average(r => r.Rating)));
}
