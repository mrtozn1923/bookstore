using Bookstore.JiraTools;
using Microsoft.Extensions.Configuration;

string[] bookstoreTransitions =
    ["Start Work", "Request Review", "Request Changes", "Approve", "Pass Test", "Fail Test", "Reopen"];

return args switch
{
    ["lint-jql", var path] => LintFilters(path),
    ["smart-commit", var path] => CheckSmartCommits(path, bookstoreTransitions),
    ["release-notes", .. var options] => await WriteReleaseNotesAsync(options),
    _ => Usage(),
};

static int LintFilters(string path)
{
    int filters = 0, warnings = 0, notes = 0;
    foreach (var (name, jql) in FilterFile.Read(path))
    {
        filters++;
        IReadOnlyList<Finding> findings = JqlLinter.Check(jql);
        Console.WriteLine(findings.Count == 0 ? $"{name}: ok" : name);
        foreach (Finding f in findings)
        {
            Console.WriteLine($"  {f.Severity.ToString().ToLowerInvariant(),-4}  {f.RuleId}  {f.Message}");
            if (f.Severity == Severity.Warn) warnings++; else notes++;
        }
    }
    Console.WriteLine($"{filters} filters, {warnings} warnings, {notes} notes");
    return warnings > 0 ? 1 : 0;
}

static int CheckSmartCommits(string path, string[] transitions)
{
    // Messages are separated by a line containing only "---".
    string[] messages = File.ReadAllText(path).Replace("\r\n", "\n").Split("\n---\n");
    int problems = 0;
    for (int m = 0; m < messages.Length; m++)
    {
        Console.WriteLine($"message {m + 1}");
        foreach (SmartCommitLine line in SmartCommit.Parse(messages[m].Trim(), transitions))
        {
            string keys = line.Keys.Count > 0 ? string.Join(" ", line.Keys) : "-";
            string commands = line.Commands.Count > 0 ? string.Join(" ", line.Commands.Select(c => "#" + c)) : "-";
            Console.WriteLine($"  line {line.Number}: keys {keys}, commands {commands}");
            foreach (string problem in line.Problems)
                Console.WriteLine($"    problem: {problem}");
            problems += line.Problems.Count;
        }
    }
    Console.WriteLine($"{messages.Length} messages, {problems} problems");
    return problems > 0 ? 1 : 0;
}

static async Task<int> WriteReleaseNotesAsync(string[] options)
{
    const string jql = "project = BOOK AND sprint in openSprints() AND statusCategory = Done "
                     + "AND hierarchyLevel = 0 ORDER BY key ASC";
    const string title = "Bookstore release notes (current sprint)";

    if (options.Contains("--sample"))
    {
        Console.Write(ReleaseNotes.Render(title, ReleaseNotes.SampleItems));
        return 0;
    }

    IConfiguration config = new ConfigurationBuilder()
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables()
        .Build();
    string? site = config["Jira:SiteUrl"], email = config["Jira:Email"], token = config["Jira:ApiToken"];
    if (site is null || email is null || token is null)
    {
        Console.Error.WriteLine("Set Jira:SiteUrl, Jira:Email and Jira:ApiToken (user secrets or Jira__* variables).");
        return 2;
    }

    Uri siteUrl = new(site.TrimEnd('/') + "/");
    if (options.Contains("--dry-run"))
    {
        Console.WriteLine($"GET {new Uri(siteUrl, JiraSearchClient.BuildSearchPath(jql)).AbsoluteUri}");
        Console.WriteLine($"Authorization: Basic <{email}:token, hidden>");
        return 0;
    }

    using HttpClient http = JiraSearchClient.CreateHttpClient(siteUrl, email, token);
    IReadOnlyList<WorkItem> items = await new JiraSearchClient(http).SearchAsync(jql);
    Console.Write(ReleaseNotes.Render(title, items));
    return 0;
}

static int Usage()
{
    Console.Error.WriteLine("usage: jiratools lint-jql <filters.txt> | smart-commit <messages.txt> | release-notes [--dry-run|--sample]");
    return 2;
}
