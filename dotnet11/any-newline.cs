using System.Text.RegularExpressions;

// .NET 11: AnyNewLine makes ^, $ and . treat \r\n, \r and \n as line breaks
string text = File.ReadAllText("data/app.log").ReplaceLineEndings("\r\n");
RegexOptions options = RegexOptions.Multiline | RegexOptions.AnyNewLine;

Console.WriteLine(Regex.Count(text, @"BKS-\d{4}-\d{6}$", options));
Console.WriteLine(Regex.Match(text, @"declined.+$", options).Value.Replace("\r", "\\r"));

// it cannot be combined with NonBacktracking or ECMAScript
try
{
    _ = new Regex("x$", options | RegexOptions.NonBacktracking);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine(ex.Message);
}
