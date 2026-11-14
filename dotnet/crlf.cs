using System.Text.RegularExpressions;

// the log file as it arrives from a Windows machine: every line ends with \r\n
string text = File.ReadAllText("data/app.log").ReplaceLineEndings("\r\n");

// $ in Multiline matches only before \n, so the \r in front of it blocks the match
Console.WriteLine(Regex.Count(text, @"BKS-\d{4}-\d{6}$", RegexOptions.Multiline));
Console.WriteLine(Regex.Count(text, @"BKS-\d{4}-\d{6}\r?$", RegexOptions.Multiline));

// . matches \r, so .+$ carries the \r into the value
string last = Regex.Match(text, @"declined.+$", RegexOptions.Multiline).Value;
Console.WriteLine(last.Replace("\r", "\\r"));
