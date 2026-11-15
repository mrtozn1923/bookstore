using Bookstore.Web.Reviews;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Web.Tests;

public sealed class RatingQueryTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.FromHours(3));

    [Fact]
    public void Midpoint_average_is_rounded_once_away_from_zero()
    {
        BookReview[] reviews =
        [
            new(1, "9780000000019", "c-1", "Ada L.", 5, "Great", Now, ReviewStatus.Approved),
            new(2, "9780000000019", "c-2", "Alan T.", 4, null, Now, ReviewStatus.Approved),
            new(3, "9780000000019", "c-3", "Grace H.", 4, null, Now, ReviewStatus.Approved),
            new(4, "9780000000019", "c-4", "Linus T.", 4, "Good", Now, ReviewStatus.Approved),
            new(5, "9780000000019", "c-5", "Ken T.", 1, "Spam", Now, ReviewStatus.Rejected),
        ];

        RatingSummary summary = Assert.Single(reviews.AsQueryable().Summaries());

        Assert.Equal(4.25, summary.Average);
        Assert.Equal(4.3m, summary.RatingValue);
        Assert.Equal((4, 2), (summary.RatingCount, summary.ReviewCount));
    }

    [Fact]
    public void Summary_query_runs_in_sql_server_as_one_grouped_statement()
    {
        DbContextOptions<ReviewsContext> options = new DbContextOptionsBuilder<ReviewsContext>().UseSqlServer().Options;
        using ReviewsContext db = new(options);

        // ToQueryString translates the query without opening a connection
        string sql = db.Reviews.Summaries().ToQueryString();
        TestContext.Current.TestOutputHelper?.WriteLine(sql);

        Assert.Contains("GROUP BY [r].[Isbn]", sql);
        Assert.Contains("AVG(CAST([r].[Rating] AS float))", sql);
    }

    private sealed class ReviewsContext(DbContextOptions<ReviewsContext> options) : DbContext(options)
    {
        public DbSet<BookReview> Reviews => Set<BookReview>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BookReview>().HasKey(r => r.Id);
            // one review per customer and book: duplicates are stopped when written, not when counted
            modelBuilder.Entity<BookReview>().HasIndex(r => new { r.Isbn, r.CustomerId }).IsUnique();
        }
    }
}
