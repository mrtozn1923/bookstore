#:property PublishAot=false
// Checks Copilot instruction, prompt, skill, agent, hook and setup-steps files: frontmatter, applyTo globs, AGENTS.md copy, size.
// Run from the repository root: dotnet run eng/check-instructions.cs
using System.Diagnostics;
using System.Text.Json;
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

foreach (string file in tracked.Where(f => f.StartsWith(".github/skills/") && f.EndsWith("/SKILL.md")))
{
    Dictionary<string, string> meta = Frontmatter(file);
    string folder = Path.GetFileName(Path.GetDirectoryName(file))!;
    string name = meta.GetValueOrDefault("name", "");
    string description = meta.GetValueOrDefault("description", "");
    string invocation = meta.GetValueOrDefault("disable-model-invocation", "false") == "true" ? "slash command only" : "auto + slash command";
    Console.WriteLine($"{file}: skill {name} ({invocation}, description {description.Length} chars)");
    if (name != folder || !Regex.IsMatch(name, "^[a-z0-9-]{1,64}$"))
    {
        errors.Add($"{file}: name '{name}' must match the folder '{folder}' and use 1-64 lowercase letters, digits or hyphens");
    }
    if (description.Length is 0 or > 1024)
    {
        errors.Add($"{file}: description must be 1-1024 characters");
    }
}

foreach (string file in tracked.Where(f => f.StartsWith(".github/agents/") && f.EndsWith(".agent.md")))
{
    Dictionary<string, string> meta = Frontmatter(file);
    Console.WriteLine($"{file}: agent {meta.GetValueOrDefault("name", Path.GetFileName(file).Replace(".agent.md", ""))} (tools: {meta.GetValueOrDefault("tools", "all")})");
    if (!meta.ContainsKey("description"))
    {
        errors.Add($"{file}: description is required for a custom agent");
    }
}

foreach (string file in tracked.Where(f => f.StartsWith(".github/hooks/") && f.EndsWith(".json")))
{
    using JsonDocument hooks = JsonDocument.Parse(File.ReadAllText(file));
    if (!hooks.RootElement.TryGetProperty("version", out JsonElement version) || version.GetInt32() != 1)
    {
        errors.Add($"{file}: \"version\": 1 is required");
    }
    foreach (JsonProperty hookEvent in hooks.RootElement.GetProperty("hooks").EnumerateObject())
    {
        foreach (JsonElement hook in hookEvent.Value.EnumerateArray())
        {
            foreach (string shell in new[] { "bash", "powershell" })
            {
                string? script = hook.TryGetProperty(shell, out JsonElement value) ? value.GetString() : null;
                if (script is not null && script.StartsWith(".github/") && !File.Exists(script))
                {
                    errors.Add($"{file}: {hookEvent.Name} {shell} script {script} does not exist");
                }
            }
        }
        Console.WriteLine($"{file}: {hookEvent.Name} -> {hookEvent.Value.GetArrayLength()} hook(s)");
    }
}

const string SetupSteps = ".github/workflows/copilot-setup-steps.yml";
if (File.Exists(SetupSteps))
{
    // Copilot reads only these keys of the copilot-setup-steps job and silently ignores the rest.
    string[] allowed = ["steps", "permissions", "runs-on", "services", "snapshot", "timeout-minutes"];
    string[] lines = File.ReadAllLines(SetupSteps);
    int job = Array.IndexOf(lines, "  copilot-setup-steps:");
    if (job < 0)
    {
        errors.Add($"{SetupSteps}: no job named copilot-setup-steps, Copilot will not run these steps");
    }
    else
    {
        List<string> keys = [];
        foreach (string line in lines.Skip(job + 1).TakeWhile(l => l.Length == 0 || l.StartsWith("    ") || l.TrimStart().StartsWith('#')))
        {
            Match key = Regex.Match(line, @"^    ([a-z-]+):\s*(.*)$");
            if (!key.Success)
            {
                continue;
            }
            keys.Add(key.Groups[1].Value);
            if (!allowed.Contains(key.Groups[1].Value))
            {
                errors.Add($"{SetupSteps}: '{key.Groups[1].Value}' is ignored by Copilot (allowed: {string.Join(", ", allowed)})");
            }
            if (key.Groups[1].Value == "timeout-minutes" && int.TryParse(key.Groups[2].Value, out int minutes) && minutes > 59)
            {
                errors.Add($"{SetupSteps}: timeout-minutes is {minutes}, the maximum is 59");
            }
        }
        Console.WriteLine($"{SetupSteps}: job copilot-setup-steps ({string.Join(", ", keys)})");
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
