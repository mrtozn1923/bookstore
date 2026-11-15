using Bookstore.Analytics.SearchConsole;

namespace Bookstore.Analytics.Cli;

public static class OpportunitiesCommand
{
    public static int Run(string csv)
    {
        CtrOpportunityResult result = CtrOpportunities.Find(QueryPageRow.ReadCsv(csv));

        Console.WriteLine("site CTR by position bucket (rows with >= 100 impressions):");
        foreach (string bucket in (string[])["1-3", "4-10", "11-20", "21+"])
        {
            if (result.BucketCtr.TryGetValue(bucket, out double ctr))
            {
                Console.WriteLine($"  {bucket,-6}{ctr,7:P1}");
            }
        }

        Console.WriteLine($"rows skipped (< 100 impressions): {result.TooLittleData}");
        Console.WriteLine();
        Console.WriteLine($"{"query",-34}{"impr",6}{"ctr",8}{"pos",6}{"bucket ctr",12}{"missed",8}");
        foreach (CtrOpportunity o in result.Opportunities)
        {
            Console.WriteLine(
                $"{o.Row.Query,-34}{o.Row.Impressions,6}{o.Row.Ctr,8:P1}{o.Row.Position,6:0.0}{o.BucketCtr,12:P1}{o.MissedClicks,8:0}");
        }

        return 0;
    }
}
