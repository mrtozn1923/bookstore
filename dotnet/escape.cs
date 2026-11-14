using System.Text.RegularExpressions;

string term = "Dune (1965).";
string text = "Dune (1965). Frank Herbert";

Console.WriteLine(Regex.Escape(term));
Console.WriteLine(Regex.IsMatch(text, "^" + Regex.Escape(term)));

try
{
    Regex.IsMatch(text, "^" + "Dune (1965");
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message);
}
