using System.Globalization;
using Bookstore.Analytics.Impact;

namespace Bookstore.Analytics.Cli;

public static class ImpactCommand
{
    // changes.csv: date,description,pages (pages separated by '|')
    public static int Run(string dailyCsv, string changesCsv, DateOnly changeDay)
    {
        string[] change = File.ReadLines(changesCsv).Skip(1)
            .Select(l => l.Split(','))
            .Single(c => DateOnly.Parse(c[0], CultureInfo.InvariantCulture) == changeDay);
        HashSet<string> testPages = [.. change[2].Split('|')];

        List<DailyPage> days = [.. ChangeImpactAnalyzer.ReadCsv(dailyCsv)];
        ImpactResult r = ChangeImpactAnalyzer.Analyze(days, testPages, changeDay);

        Console.WriteLine($"change {changeDay:yyyy-MM-dd}: {change[1]} ({testPages.Count} pages)");
        Console.WriteLine($"windows: 28 days before vs 28 days after, change day excluded");
        Console.WriteLine();
        Console.WriteLine($"{"",-9}{"clicks before",15}{"after",8}{"change",9}{"ctr before",12}{"after",8}");
        Row("test", r.TestBefore, r.TestAfter);
        Row("control", r.ControlBefore, r.ControlAfter);
        Console.WriteLine();
        Console.WriteLine($"test vs control, clicks: {r.RelativeEffect * 100:+0.0;-0.0}%   ctr: {r.RelativeCtrEffect * 100:+0.0;-0.0}%");
        Console.WriteLine($"weekly test/control ratio, 12 weeks before: {r.BaselineMin:0.000} .. {r.BaselineMax:0.000}");
        Console.WriteLine($"weekly test/control ratio, 4 weeks after:  {string.Join("  ", r.AfterWeeklyRatios.Select(x => x.ToString("0.000")))}");
        Console.WriteLine(r.OutsideNormalVariation
            ? "every week after the change is outside the range of the 12 weeks before it"
            : "the weeks after the change stay within normal variation: no evidence of an effect");
        return 0;
    }

    private static void Row(string name, GroupWindow before, GroupWindow after) =>
        Console.WriteLine($"{name,-9}{before.Clicks,15}{after.Clicks,8}" +
                          $"{(after.Clicks - before.Clicks) * 100.0 / before.Clicks,8:+0.0;-0.0}%" +
                          $"{before.Ctr * 100,11:0.00}%{after.Ctr * 100,7:0.00}%");
}
