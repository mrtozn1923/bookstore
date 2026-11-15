#:property PublishAot=false
// Shows what Copilot code review works with for a pull request: the files it reviews, the files it skips
// and the instruction files it reads from the head branch. From the repository root:
// dotnet run eng/review-context.cs [base] [head]      (defaults: main HEAD)
using System.Diagnostics;
using System.Text.RegularExpressions;

string baseRef = args.ElementAtOrDefault(0) ?? "main";
string headRef = args.ElementAtOrDefault(1) ?? "HEAD";

// Part of the list in "Files excluded from GitHub Copilot code review" (docs.github.com, November 2026).
string[] excludedNames = [".gitignore", "package-lock.json", "yarn.lock", "tsconfig.json", "jest.config.js", "requirements.txt", "paket.lock"];
string[] excludedGlobs = ["**/*.svg", "**/*.log", "**/*.lock", "**/dist/**", "**/node_modules/**"];
string[] guidanceFiles = [".github/copilot-instructions.md", "AGENTS.md", "CLAUDE.md", "GEMINI.md", "REVIEW.md"];

string[] changed = await Git($"diff --name-only {baseRef}...{headRef}");
string[] headFiles = await Git($"ls-tree -r --name-only {headRef}");
string[] skipped = changed.Where(f => excludedNames.Contains(Path.GetFileName(f)) || excludedGlobs.Any(g => GlobToRegex(g).IsMatch(f))).ToArray();
string[] reviewed = changed.Except(skipped).ToArray();

Console.WriteLine($"{baseRef}...{headRef}: {changed.Length} changed file(s)");
Console.WriteLine($"Reviewed ({reviewed.Length}): {string.Join(", ", reviewed)}");
Console.WriteLine($"Skipped by Copilot code review ({skipped.Length}): {string.Join(", ", skipped)}");

Console.WriteLine($"Instructions read from {headRef}:");
int characters = 0;
foreach (string file in guidanceFiles.Where(headFiles.Contains))
{
    characters += (await Show(headRef, file)).Length;
    Console.WriteLine($"  always  {file}");
}
foreach (string file in headFiles.Where(f => f.StartsWith(".github/instructions/") && f.EndsWith(".instructions.md")))
{
    string content = await Show(headRef, file);
    string applyTo = Field(content, "applyTo");
    if (Field(content, "excludeAgent") == "code-review")
    {
        Console.WriteLine($"  skip    {file} (excludeAgent: code-review)");
        continue;
    }
    int matches = reviewed.Count(f => applyTo.Split(',', StringSplitOptions.TrimEntries).Any(g => GlobToRegex(g).IsMatch(f)));
    if (matches > 0)
    {
        characters += content.Length;
    }
    Console.WriteLine(matches > 0 ? $"  path    {file} (applyTo {applyTo} -> {matches} reviewed file(s))" : $"  skip    {file} (applyTo {applyTo} matches no reviewed file)");
}
Console.WriteLine($"  {characters:N0} characters of instructions");

string[] skills = headFiles.Where(f => f.StartsWith(".github/skills/") && f.EndsWith("/SKILL.md")).Select(f => f.Split('/')[2]).ToArray();
Console.WriteLine($"Skills: {string.Join(", ", skills.Select(s => s == "code-review" ? s + " (review-focused name)" : s))}");

foreach (string file in changed.Where(f => f.StartsWith(".github/instructions/") || f.StartsWith(".github/skills/") || guidanceFiles.Contains(f)))
{
    Console.WriteLine($"warning: this pull request changes {file}; Copilot reviews it with the changed version");
}

static async Task<string[]> Git(string arguments) =>
    (await Run(arguments)).Split('\n', StringSplitOptions.RemoveEmptyEntries);

static Task<string> Show(string reference, string path) => Run($"show {reference}:{path}");

static async Task<string> Run(string arguments)
{
    ProcessStartInfo start = new("git", arguments) { RedirectStandardOutput = true };
    using Process process = Process.Start(start)!;
    string output = await process.StandardOutput.ReadToEndAsync();
    await process.WaitForExitAsync();
    return output;
}

static string Field(string content, string name)
{
    Match match = Regex.Match(content, $@"^{name}:\s*""?([^""\r\n]*)""?\s*$", RegexOptions.Multiline);
    return match.Success ? match.Groups[1].Value.Trim() : "";
}

// "**" crosses folders, "*" and "?" stay inside one path segment.
static Regex GlobToRegex(string glob) =>
    new("^" + Regex.Escape(glob)
        .Replace(@"\*\*/", "(.*/)?")
        .Replace(@"\*\*", ".*")
        .Replace(@"\*", "[^/]*")
        .Replace(@"\?", "[^/]") + "$");
