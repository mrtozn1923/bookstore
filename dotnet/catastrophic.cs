using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// ^(a+)+$ on n letters followed by "!": the backtracking engine tries every way to split the letters
Regex backtracking = new(@"^(a+)+$");
Regex nonBacktracking = new(@"^(a+)+$", RegexOptions.NonBacktracking);
Regex rewritten = new(@"^a+$");
foreach (Regex regex in new[] { backtracking, nonBacktracking, rewritten })
    regex.IsMatch("aaaa!"); // warm-up

Console.WriteLine($"{"n",3} {"backtracking ms",16} {"NonBacktracking ms",19} {"^a+$ ms",8}");
for (int n = 16; n <= 28; n += 2)
{
    string input = new string('a', n) + "!";
    Console.WriteLine($"{n,3} {Time(backtracking, input),16:F1} {Time(nonBacktracking, input),19:F3} {Time(rewritten, input),8:F3}");
}

static double Time(Regex regex, string input)
{
    Stopwatch sw = Stopwatch.StartNew();
    regex.IsMatch(input);
    return sw.Elapsed.TotalMilliseconds;
}
