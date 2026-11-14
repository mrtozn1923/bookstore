using System.Text.RegularExpressions;

// \d, \w and \s are Unicode-aware in .NET unless RegexOptions.ECMAScript is set
Console.WriteLine(Regex.IsMatch("٣", @"^\d$"));                               // Arabic-Indic three
Console.WriteLine(Regex.IsMatch("٣", @"^\d$", RegexOptions.ECMAScript));
Console.WriteLine(Regex.IsMatch("Şükrü", @"^\w+$"));
Console.WriteLine(Regex.IsMatch("Şükrü", @"^\w+$", RegexOptions.ECMAScript));
Console.WriteLine(Regex.IsMatch(" ", @"^\s$"));                          // no-break space

// [A-z] is not "all letters": it also spans [ \ ] ^ _ `
Console.WriteLine(string.Concat(Regex.Matches("A_z^`[", "[A-z]").Select(m => m.Value)));

// an unescaped dot matches any character except \n
Console.WriteLine(Regex.IsMatch("orders@exampleXcom", @"@example.com$"));
Console.WriteLine(Regex.IsMatch("orders@exampleXcom", @"@example\.com$"));
Console.WriteLine(Regex.IsMatch("Dune\nEmma", "Dune.Emma"));
Console.WriteLine(Regex.IsMatch("Dune\nEmma", "Dune.Emma", RegexOptions.Singleline));
