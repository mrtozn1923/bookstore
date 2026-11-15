using System.Globalization;

namespace Bookstore.Analytics.Consent;

public sealed record CoverageDay(DateOnly Date, int LogGoogleLandings, int Ga4OrganicSessions);

public sealed record CoverageWeek(DateOnly From, int LogGoogleLandings, int Ga4OrganicSessions)
{
    // Sessions and landing requests are different units: watch how the ratio moves, not its level.
    public double Coverage => LogGoogleLandings == 0 ? 0 : (double)Ga4OrganicSessions / LogGoogleLandings;
}

public static class CoverageEstimator
{
    public static IEnumerable<CoverageDay> ReadCsv(string path) =>
        File.ReadLines(path).Skip(1).Where(l => l.Length > 0).Select(l => l.Split(',')).Select(c => new CoverageDay(
            DateOnly.Parse(c[0], CultureInfo.InvariantCulture),
            int.Parse(c[1], CultureInfo.InvariantCulture),
            int.Parse(c[2], CultureInfo.InvariantCulture)));

    // Whole weeks only, starting from the first day, so weekdays stay comparable.
    public static List<CoverageWeek> Weekly(IEnumerable<CoverageDay> days)
    {
        List<CoverageDay> ordered = days.OrderBy(d => d.Date).ToList();
        return ordered
            .Select((day, index) => (day, week: index / 7))
            .GroupBy(x => x.week)
            .Where(g => g.Count() == 7)
            .Select(g => new CoverageWeek(
                g.First().day.Date,
                g.Sum(x => x.day.LogGoogleLandings),
                g.Sum(x => x.day.Ga4OrganicSessions)))
            .ToList();
    }
}
