#:property PublishAot=false
using System.Text.RegularExpressions;

// Finds paths written as `code` in every AGENTS.md and checks that they still exist.
string root = Directory.GetCurrentDirectory();
Regex codeSpan = new(@"`([^`\s]+)`");
int missing = 0;

foreach (string file in Directory.EnumerateFiles(root, "AGENTS.md", SearchOption.AllDirectories)
             .Where(f => !f.Contains("node_modules")))
{
    string dir = Path.GetDirectoryName(file)!;
    foreach (Match m in codeSpan.Matches(File.ReadAllText(file)))
    {
        string candidate = m.Groups[1].Value.TrimEnd('/');
        // Only relative paths such as `src/Bookstore.Api/Data/`; routes (/api) and placeholders (<Name>) are skipped
        bool looksLikePath = candidate.Contains('/') && !candidate.StartsWith('/');
        if (!looksLikePath || candidate.Contains('<') || candidate.Contains("://"))
        {
            continue;
        }
        bool exists = new[] { Path.Combine(dir, candidate), Path.Combine(root, candidate) }
            .Any(p => File.Exists(p) || Directory.Exists(p));
        if (!exists)
        {
            missing++;
            Console.WriteLine($"MISSING {Path.GetRelativePath(root, file)}: {candidate}");
        }
    }
}

Console.WriteLine(missing == 0 ? "All paths in AGENTS.md files exist." : $"{missing} stale path(s).");
return missing == 0 ? 0 : 1;
