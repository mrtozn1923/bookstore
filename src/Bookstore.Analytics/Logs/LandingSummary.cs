namespace Bookstore.Analytics.Logs;

public sealed record LandingSummary(
    IReadOnlyDictionary<TrafficSource, int> BySource,
    IReadOnlyList<(string Path, int Landings)> TopGoogleLandings,
    int Unparsed);

public static class LandingReport
{
    public static LandingSummary Build(IEnumerable<string> lines, TrafficClassifier classifier, int top = 5)
    {
        Dictionary<TrafficSource, int> bySource = [];
        Dictionary<string, int> google = [];
        int unparsed = 0;

        foreach (string line in lines)
        {
            AccessLogEntry? entry = CombinedLogParser.Parse(line);
            if (entry is null)
            {
                unparsed++;
                continue;
            }

            TrafficSource source = classifier.Classify(entry);
            bySource[source] = bySource.GetValueOrDefault(source) + 1;
            if (source == TrafficSource.GoogleSearch)
            {
                string path = entry.Path.Split('?', 2)[0];
                google[path] = google.GetValueOrDefault(path) + 1;
            }
        }

        List<(string, int)> topGoogle = google
            .OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal)
            .Take(top)
            .Select(p => (p.Key, p.Value))
            .ToList();

        return new LandingSummary(bySource, topGoogle, unparsed);
    }
}
