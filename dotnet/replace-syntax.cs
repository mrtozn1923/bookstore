using System.Text.RegularExpressions;

string price = "Emma: 45";

// group 1 followed by a literal 0?
Console.WriteLine(Regex.Replace(price, @"(\d+)", "$10 TRY"));
Console.WriteLine(Regex.Replace(price, @"(\d+)", "${1}0 TRY"));
// $$ is a literal dollar sign, $0 the whole match, a missing group stays as text
Console.WriteLine(Regex.Replace(price, @"(\d+)", "$$$1"));
Console.WriteLine(Regex.Replace(price, @"(\d+)", "[$0]"));
Console.WriteLine(Regex.Replace(price, @"(\d+)", "$2"));
