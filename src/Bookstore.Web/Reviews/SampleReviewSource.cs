namespace Bookstore.Web.Reviews;

// Sample data; in Bookstore the same IQueryable comes from BookstoreContext
public sealed class SampleReviewSource : IReviewSource
{
    private static readonly TimeSpan Istanbul = TimeSpan.FromHours(3);

    public IReadOnlyList<BookReview> All { get; init; } =
    [
        new(1, "9780000000019", "c-101", "Ada L.", 5, "Still the best world-building I have read.", new(2026, 10, 2, 20, 15, 0, Istanbul), ReviewStatus.Approved),
        new(2, "9780000000019", "c-102", "Alan T.", 4, "Slow first hundred pages, then impossible to put down.", new(2026, 10, 9, 9, 40, 0, Istanbul), ReviewStatus.Approved),
        new(3, "9780000000019", "c-103", "Grace H.", 5, "Good paper and a readable font size.", new(2026, 10, 21, 18, 5, 0, Istanbul), ReviewStatus.Approved, IsIncentivized: true),
        new(4, "9780000000019", "c-104", "Linus T.", 3, "The appendices are more interesting than the ending.", new(2026, 11, 3, 22, 30, 0, Istanbul), ReviewStatus.Approved),
        new(5, "9780000000019", "c-105", "Barbara L.", 4, null, new(2026, 11, 4, 11, 0, 0, Istanbul), ReviewStatus.Approved),
        new(6, "9780000000019", "c-106", "Edsger D.", 5, null, new(2026, 11, 6, 8, 20, 0, Istanbul), ReviewStatus.Approved),
        new(7, "9780000000019", "c-107", "Margaret H.", 4, null, new(2026, 11, 8, 14, 45, 0, Istanbul), ReviewStatus.Approved),
        new(8, "9780000000019", "c-108", "Ken T.", 1, "Cheap watches here: example.com/deal", new(2026, 11, 9, 3, 12, 0, Istanbul), ReviewStatus.Rejected),
        new(9, "9780000000019", "c-109", "Dennis R.", 2, "Arrived with a bent cover.", new(2026, 11, 12, 16, 0, 0, Istanbul), ReviewStatus.Pending),
    ];

    public IQueryable<BookReview> Reviews => All.AsQueryable();
}
