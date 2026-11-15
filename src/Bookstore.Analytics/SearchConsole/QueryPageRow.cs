using System.Globalization;

namespace Bookstore.Analytics.SearchConsole;

// One row of a page + query export (UI export, API or bulk export).
public sealed record QueryPageRow(string Page, string Query, int Clicks, int Impressions, double Position)
{
    public double Ctr => Impressions == 0 ? 0 : (double)Clicks / Impressions;

    public static IEnumerable<QueryPageRow> ReadCsv(string path) =>
        File.ReadLines(path)
            .Skip(1)
            .Where(l => l.Length > 0)
            .Select(l => l.Split(','))
            .Select(c => new QueryPageRow(
                c[0], c[1],
                int.Parse(c[2], CultureInfo.InvariantCulture),
                int.Parse(c[3], CultureInfo.InvariantCulture),
                double.Parse(c[4], CultureInfo.InvariantCulture)));
}
