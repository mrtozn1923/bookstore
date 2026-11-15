#:property PublishAot=false
// Rough context cost of files tracked by git: about 4 characters per token.
// Run: dotnet run eng/context-size.cs -- [path prefix ...]
using System.Diagnostics;
using System.Globalization;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string[] prefixes = args.Length > 0 ? args : [""];
ProcessStartInfo git = new("git", "ls-files") { RedirectStandardOutput = true };
using Process process = Process.Start(git)!;
string[] tracked = (await process.StandardOutput.ReadToEndAsync())
    .Split('\n', StringSplitOptions.RemoveEmptyEntries);
await process.WaitForExitAsync();

List<(string Path, int Tokens)> files =
[
    .. tracked
        .Where(p => prefixes.Any(prefix => p.StartsWith(prefix, StringComparison.Ordinal)))
        .Select(p => (p, File.ReadAllText(p).Length / 4))
        .OrderByDescending(f => f.Item2)
];

Console.WriteLine($"{files.Count} files, ~{files.Sum(f => f.Tokens):N0} tokens");
foreach ((string path, int tokens) in files.Take(5))
{
    Console.WriteLine($"{tokens,10:N0}  {path}");
}
