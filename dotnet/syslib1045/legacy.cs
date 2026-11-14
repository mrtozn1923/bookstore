using System.Text.RegularExpressions;

// constant patterns built at run time: the analyzer suggests GeneratedRegex for both
Regex orderNumber = new("BKS-[0-9]{4}-[0-9]{6}");
Console.WriteLine(orderNumber.IsMatch("BKS-2026-000123"));
Console.WriteLine(Regex.IsMatch("978-0-441-17271-9", "^97[89]-"));
