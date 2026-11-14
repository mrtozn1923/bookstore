#:property PublishAot=false
// Usage:
//   gh pr list --state merged --limit 200 --json number,author,createdAt,mergedAt,additions,deletions,reviews > prs.json
//   dotnet run eng/review-metrics.cs -- prs.json
using System.Globalization;
using System.Text.Json;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
List<PullRequest> prs = JsonSerializer.Deserialize<List<PullRequest>>(File.ReadAllText(args[0]), json) ?? [];

// Bot PRs (dependency updates) have a different review flow; keep them out of team numbers.
List<PullRequest> human = [.. prs.Where(p => !p.Author.Login.EndsWith("[bot]"))];

Console.WriteLine($"{"PR",-5} {"author",-7} {"lines",6} {"first review (h)",17} {"rounds",7}");
List<double> waits = [];
foreach (PullRequest pr in human.OrderBy(p => p.Number))
{
    Review? first = pr.Reviews
        .Where(r => r.Author.Login != pr.Author.Login)
        .OrderBy(r => r.SubmittedAt)
        .FirstOrDefault();
    double? wait = first is null ? null : (first.SubmittedAt - pr.CreatedAt).TotalHours;
    if (wait is double w)
    {
        waits.Add(w);
    }
    int rounds = pr.Reviews.Count(r => r.State == "CHANGES_REQUESTED") + 1;
    Console.WriteLine($"#{pr.Number,-4} {pr.Author.Login,-7} {pr.Additions + pr.Deletions,6} {wait?.ToString("0.0") ?? "-",17} {rounds,7}");
}

Console.WriteLine();
Console.WriteLine($"median time to first review: {Median(waits):0.0} h");
Console.WriteLine($"median PR size:              {Median([.. human.Select(p => (double)(p.Additions + p.Deletions))]):0} lines");
Console.WriteLine($"PRs over 400 lines:          {human.Count(p => p.Additions + p.Deletions > 400)} of {human.Count}");
Console.WriteLine("reviews given:               " + string.Join(", ", human
    .SelectMany(p => p.Reviews.Where(r => r.Author.Login != p.Author.Login).Select(r => (p.Number, r.Author.Login)))
    .Distinct()
    .GroupBy(x => x.Login)
    .OrderByDescending(g => g.Count())
    .Select(g => $"{g.Key} {g.Count()}")));

static double Median(List<double> values)
{
    if (values.Count == 0)
    {
        return 0;
    }
    List<double> sorted = [.. values.Order()];
    int middle = sorted.Count / 2;
    return sorted.Count % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
}

record Author(string Login);
record Review(Author Author, string State, DateTimeOffset SubmittedAt);
record PullRequest(
    int Number, Author Author, DateTimeOffset CreatedAt, DateTimeOffset? MergedAt, int Additions, int Deletions,
    List<Review> Reviews);
