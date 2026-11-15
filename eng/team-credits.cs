#:property PublishAot=false
// Monthly AI Credits of a team against the shared pool of Copilot Business or Enterprise.
// Session credits: eng/ai-credits.cs (30-step agent session). Review credits: average per review
// on docs.github.com (Lite 0.25, Balanced 1.50, Max 10 USD), checked 2026-11-15. 1 AI credit = 0.01 USD.
// Run: dotnet run eng/team-credits.cs -- [developers] [business|enterprise]
using System.Globalization;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

int developers = args.Length > 0 ? int.Parse(args[0]) : 8;
bool enterprise = args.Length > 1 && args[1] == "enterprise";
int poolPerSeat = enterprise ? 3_900 : 1_900;
decimal seatPrice = enterprise ? 39m : 19m;
const int WorkingDays = 21;
const int SessionsPerDay = 4;
const int PullRequestsPerMonth = 12;   // per developer
const int PushesPerPullRequest = 6;    // reviews per pull request when "Review new pushes" is on
int pool = developers * poolPerSeat;

Scenario[] scenarios =
[
    new("Luna sessions, Lite review once", SessionCredits: 4, ReviewCredits: 25, ReviewsPerPullRequest: 1),
    new("Sonnet sessions, Balanced review once", 64, 150, 1),
    new("Sonnet sessions, Balanced on every push", 64, 150, PushesPerPullRequest),
    new("Opus sessions, Max on every push", 128, 1_000, PushesPerPullRequest),
];

Console.WriteLine($"{developers} developers, Copilot {(enterprise ? "Enterprise" : "Business")}: seats {developers * seatPrice:N0} USD, pool {pool:N0} AI credits per month");
Console.WriteLine($"{"Scenario",-42}{"Sessions",10}{"Reviews",10}{"Total",10}{"Pool",9}{"Extra USD",11}");
foreach (Scenario s in scenarios)
{
    long sessions = (long)s.SessionCredits * SessionsPerDay * WorkingDays * developers;
    long reviews = (long)s.ReviewCredits * s.ReviewsPerPullRequest * PullRequestsPerMonth * developers;
    long total = sessions + reviews;
    decimal extra = Math.Max(0, total - pool) / 100m;
    Console.WriteLine($"{s.Name,-42}{sessions,10:N0}{reviews,10:N0}{total,10:N0}{(decimal)total / pool,9:P0}{extra,11:N2}");
}
Console.WriteLine("GitHub Actions minutes for code review and the cloud agent are not included.");

record Scenario(string Name, int SessionCredits, int ReviewCredits, int ReviewsPerPullRequest);
