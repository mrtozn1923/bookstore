#:property PublishAot=false
// Checks Copilot instruction and prompt files: frontmatter, applyTo globs, AGENTS.md copy, size.
// Run from the repository root: dotnet run eng/check-instructions.cs
using System.Diagnostics;
using System.Text.RegularExpressions;

const int AlwaysLoadedBudget = 8_000;   // our own budget in characters, not a Copilot limit
List<string> errors = [];

ProcessStartInfo git = new("git", "ls-files") { RedirectStandardOutput = true };
using Process process = Process.Start(git)!;
string[] tracked = (await process.StandardOutput.ReadToEndAsync()).Split('\n', StringSplitOptions.RemoveEmptyEntries);
await process.WaitForExitAsync();

foreach (string file in tracked.Where(f => f.StartsWith(".github/instructions/") && f.EndsWith(".instructions.md")))
{
    Dictionary<string, string> meta = Frontmatter(file);
    if (!meta.TryGetValue("applyTo", out string? applyTo))
    {
        errors.Add($"{file}: no applyTo, the file is never applied automatically");
        continue;
    }
    foreach (string glob in applyTo.Split(',', StringSplitOptions.TrimEntries))
    {
        int matches = tracked.Count(f => GlobToRegex(glob).IsMatch(f));
        Console.WriteLine($"{file}: applyTo {glob} -> {matches} files");
        if (matches == 0)
        {
            errors.Add($"{file}: applyTo '{glob}' matches no tracked file");
        }
    }
    if (meta.TryGetValue("excludeAgent", out string? agent) && agent is not ("code-review" or "cloud-agent"))
    {
        errors.Add($"{file}: excludeAgent must be code-review or cloud-agent, not '{agent}'");
    }
}

foreach (string file in tracked.Where(f => f.StartsWith(".github/prompts/") && f.EndsWith(".prompt.md")))
{
    Dictionary<string, string> meta = Frontmatter(file);
    Console.WriteLine($"{file}: /{meta.GetValueOrDefault("name", Path.GetFileName(file).Replace(".prompt.md", ""))} (agent: {meta.GetValueOrDefault("agent", "current")})");
    if (!meta.ContainsKey("description"))
    {
        errors.Add($"{file}: description is missing");
    }
}

string agentsRules = Rules("AGENTS.md");
string copilotRules = Rules(".github/copilot-instructions.md");
if (agentsRules != copilotRules)
{
    errors.Add(".github/copilot-instructions.md: rules differ from AGENTS.md, copy the Rules section again");
}

long alwaysLoaded = new FileInfo("AGENTS.md").Length + new FileInfo(".github/copilot-instructions.md").Length;
Console.WriteLine($"Always loaded (AGENTS.md + copilot-instructions.md): {alwaysLoaded:N0} characters, budget {AlwaysLoadedBudget:N0}");
if (alwaysLoaded > AlwaysLoadedBudget)
{
    errors.Add($"always-loaded instructions are {alwaysLoaded:N0} characters, over the {AlwaysLoadedBudget:N0} budget");
}

foreach (string error in errors)
{
    Console.Error.WriteLine($"error: {error}");
}
Console.WriteLine(errors.Count == 0 ? "OK" : $"{errors.Count} error(s)");
return errors.Count == 0 ? 0 : 1;

static Dictionary<string, string> Frontmatter(string path)
{
    string[] lines = File.ReadAllLines(path);
    Dictionary<string, string> values = [];
    if (lines.Length == 0 || lines[0] != "---")
    {
        return values;
    }
    foreach (string line in lines.Skip(1).TakeWhile(l => l != "---"))
    {
        int colon = line.IndexOf(':');
        if (colon > 0)
        {
            values[line[..colon].Trim()] = line[(colon + 1)..].Trim().Trim('"', '\'');
        }
    }
    return values;
}

static string Rules(string path)
{
    Match match = Regex.Match(File.ReadAllText(path), "<!-- rules:start -->(.*?)<!-- rules:end -->", RegexOptions.Singleline);
    return match.Success ? match.Groups[1].Value.Trim() : "";
}

// "**" spans folders, "*" and "?" stay inside one path segment.
static Regex GlobToRegex(string glob) =>
    new("^" + Regex.Escape(glob)
        .Replace(@"\*\*/", "(.*/)?")
        .Replace(@"\*\*", ".*")
        .Replace(@"\*", "[^/]*")
        .Replace(@"\?", "[^/]") + "$");
