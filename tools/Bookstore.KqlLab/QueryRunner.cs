using System.Globalization;
using System.Text.Json.Nodes;
using KustoLoco.Core;

namespace Bookstore.KqlLab;

// Runs one .kql file against the captured telemetry and prints the result as a text table.
public static class QueryRunner
{
    public static async Task<int> RunAsync(string queryFile, string dataDir)
    {
        KustoQueryContext context = AppInsightsTables.Load(dataDir);
        string query = await File.ReadAllTextAsync(queryFile);
        KustoQueryResult result = await context.RunQuery(query);
        if (!string.IsNullOrEmpty(result.Error))
        {
            Console.Error.WriteLine(result.Error);
            return 2;
        }

        string[] names = result.ColumnNames();
        List<string[]> rows = [.. result.EnumerateRows().Select(row => row.Select(Format).ToArray())];
        int[] widths = [.. names.Select((n, i) => Math.Max(n.Length, rows.Count == 0 ? 0 : rows.Max(r => r[i].Length)))];
        Console.WriteLine(string.Join("  ", names.Select((n, i) => n.PadRight(widths[i]))).TrimEnd());
        Console.WriteLine(string.Join("  ", widths.Select(w => new string('-', w))));
        foreach (string[] row in rows)
        {
            Console.WriteLine(string.Join("  ", row.Select((v, i) => v.PadRight(widths[i]))).TrimEnd());
        }
        if (result.IsChart)
        {
            // The local engine keeps the render request as metadata; the portal would draw the chart.
            Console.WriteLine($"render: {result.Visualization.ChartType}");
        }
        return 0;
    }

    private static string Format(object? value) => value switch
    {
        null => "",
        DateTime t when t.TimeOfDay.Ticks % TimeSpan.TicksPerSecond == 0 => t.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
        DateTime t => t.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
        TimeSpan s => s.ToString("c", CultureInfo.InvariantCulture),
        double d => d.ToString("0.##", CultureInfo.InvariantCulture),
        JsonNode j => j.ToJsonString(),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? ""
    };
}
