using System.Diagnostics;
using System.Text.RegularExpressions;

// a process-wide default timeout; it is read once, so set it before the first Regex is used
AppContext.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(2));

// a nested quantifier and an input that almost matches
string input = new string('a', 30) + "!";

// 1. timeout: the backtracking engine gives up after 100 ms
Regex withTimeout = new(@"^(a+)+$", RegexOptions.None, TimeSpan.FromMilliseconds(100));
try
{
    withTimeout.IsMatch(input);
}
catch (RegexMatchTimeoutException ex)
{
    Console.WriteLine($"{ex.GetType().Name}: {ex.MatchTimeout.TotalMilliseconds} ms");
}

// 2. NonBacktracking: same pattern, same input, linear time
Stopwatch sw = Stopwatch.StartNew();
bool matched = Regex.IsMatch(input, @"^(a+)+$", RegexOptions.NonBacktracking);
Console.WriteLine($"NonBacktracking: {matched} in under 100 ms: {sw.ElapsedMilliseconds < 100}");

// 3. constructs that need backtracking are rejected when the Regex is constructed
foreach (string pattern in new[] { @"Payment (?=\d)", @"\b(\w+) \1\b", @"(?>\d+)0", @"(?<=BKS-)\d+" })
{
    try
    {
        _ = new Regex(pattern, RegexOptions.NonBacktracking);
        Console.WriteLine($"{pattern,-16} ok");
    }
    catch (NotSupportedException ex)
    {
        Console.WriteLine($"{pattern,-16} {ex.Message}");
    }
}

// 4. every Regex created without a timeout gets the process-wide default
Console.WriteLine($"default timeout: {new Regex("BKS").MatchTimeout}");
