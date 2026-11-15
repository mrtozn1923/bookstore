using System.Text.RegularExpressions;
using Bookstore.Analytics.SearchConsole;

namespace Bookstore.Analytics.Tests.SearchConsole;

public class CtrOpportunitiesTests
{
    [Theory]
    [InlineData(1.0, "1-3")]
    [InlineData(3.4, "1-3")]
    [InlineData(3.5, "4-10")]
    [InlineData(10.5, "11-20")]
    [InlineData(25.0, "21+")]
    public void Buckets(double position, string bucket) => Assert.Equal(bucket, CtrOpportunities.BucketOf(position));

    [Fact]
    public void Low_ctr_row_in_top_ten_is_flagged_and_low_volume_rows_are_skipped()
    {
        QueryPageRow[] rows =
        [
            new("/a", "good", 100, 1000, 2.0),
            new("/b", "weak", 10, 1000, 2.0),
            new("/c", "tiny", 0, 20, 2.0),
            new("/d", "far", 1, 1000, 15.0),
        ];

        CtrOpportunityResult result = CtrOpportunities.Find(rows);

        CtrOpportunity only = Assert.Single(result.Opportunities);
        Assert.Equal("weak", only.Row.Query);
        Assert.Equal(0.055, result.BucketCtr["1-3"], 3);
        Assert.Equal(45, only.MissedClicks, 3);
        Assert.Equal(1, result.TooLittleData);
    }

    [Fact]
    public void DotNet_regex_word_boundary_is_unicode_unlike_re2()
    {
        // .NET \b is Unicode-aware; RE2 (Search Console) treats only ASCII as word characters.
        Assert.Matches(new Regex(@"\b(örnek|örneği)\b"), "dapper örnek proje");
    }
}
