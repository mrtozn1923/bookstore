using Bookstore.Analytics.Consent;

namespace Bookstore.Analytics.Cli;

public static class CoverageCommand
{
    public static int Run(string csv)
    {
        List<CoverageWeek> weeks = CoverageEstimator.Weekly(CoverageEstimator.ReadCsv(csv));

        Console.WriteLine($"{"week from",-12}{"log landings",14}{"ga4 sessions",14}{"coverage",10}");
        foreach (CoverageWeek w in weeks)
        {
            Console.WriteLine($"{w.From:yyyy-MM-dd}  {w.LogGoogleLandings,12}{w.Ga4OrganicSessions,14}{w.Coverage * 100,9:0}%");
        }

        CoverageWeek first = weeks[0];
        CoverageWeek last = weeks[^1];
        Console.WriteLine();
        Console.WriteLine($"ga4 sessions {Change(first.Ga4OrganicSessions, last.Ga4OrganicSessions)}, " +
                          $"log landings {Change(first.LogGoogleLandings, last.LogGoogleLandings)}");
        return 0;
    }

    private static string Change(int before, int after) => $"{(after - before) * 100.0 / before:+0;-0}%";
}
