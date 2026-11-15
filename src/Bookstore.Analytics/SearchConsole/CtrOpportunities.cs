namespace Bookstore.Analytics.SearchConsole;

public sealed record CtrOpportunity(QueryPageRow Row, string Bucket, double BucketCtr, double MissedClicks);

public sealed record CtrOpportunityResult(
    IReadOnlyDictionary<string, double> BucketCtr,
    IReadOnlyList<CtrOpportunity> Opportunities,
    int TooLittleData);

public static class CtrOpportunities
{
    public static string BucketOf(double position) => position switch
    {
        < 3.5 => "1-3",
        < 10.5 => "4-10",
        < 20.5 => "11-20",
        _ => "21+",
    };

    // Compares every row with the site's own CTR for the same position bucket,
    // not with an industry curve.
    public static CtrOpportunityResult Find(IEnumerable<QueryPageRow> rows, int minImpressions = 100, double ratio = 0.5)
    {
        List<QueryPageRow> enough = [];
        int tooLittle = 0;
        foreach (QueryPageRow row in rows)
        {
            if (row.Impressions >= minImpressions)
            {
                enough.Add(row);
            }
            else
            {
                tooLittle++;
            }
        }

        Dictionary<string, double> bucketCtr = enough
            .GroupBy(r => BucketOf(r.Position))
            .ToDictionary(g => g.Key, g => (double)g.Sum(r => r.Clicks) / g.Sum(r => r.Impressions));

        List<CtrOpportunity> opportunities = enough
            .Where(r => r.Position < 10.5)
            .Select(r => (Row: r, Bucket: BucketOf(r.Position)))
            .Where(x => x.Row.Ctr < bucketCtr[x.Bucket] * ratio)
            .Select(x => new CtrOpportunity(x.Row, x.Bucket, bucketCtr[x.Bucket],
                x.Row.Impressions * (bucketCtr[x.Bucket] - x.Row.Ctr)))
            .OrderByDescending(o => o.MissedClicks)
            .ToList();

        return new CtrOpportunityResult(bucketCtr, opportunities, tooLittle);
    }
}
