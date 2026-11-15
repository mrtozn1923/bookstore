using Microsoft.Extensions.Caching.Memory;

namespace Bookstore.Web.Reviews;

public interface IReviewSource
{
    IQueryable<BookReview> Reviews { get; }
}

// Summary and visible reviews are cached together, so their counts always agree
public sealed record ReviewSection(RatingSummary? Summary, IReadOnlyList<BookReview> Latest);

public sealed class ReviewService(IReviewSource source, IMemoryCache cache)
{
    public ReviewSection ForBook(string isbn) =>
        cache.GetOrCreate($"reviews:{isbn}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            IQueryable<BookReview> forBook = source.Reviews.Where(r => r.Isbn == isbn);

            RatingSummary? summary = forBook.Summaries().SingleOrDefault();
            List<BookReview> latest = forBook
                .Where(r => r.Status == ReviewStatus.Approved && r.Text != null)
                .OrderByDescending(r => r.CreatedAt)
                .Take(3)
                .ToList();
            return new ReviewSection(summary, latest);
        })!;
}
